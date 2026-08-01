"""Cliente Postgres (Neon) — só leitura/escrita de linha.

Nenhuma criação/alteração de tabela acontece aqui: o schema é propriedade do
lado C# (versionado via EF Core Migrations). Este módulo só sabe inserir e
consultar linhas nas tabelas que já existem.

Decisão de driver (mesmo padrão de decisão explícita já usado no projeto —
ver a escolha do Npgsql pro lado C#): **psycopg (v3)**, não asyncpg. As
funções daqui são pontuais (poucos inserts/queries, não um servidor de alto
throughput) — psycopg dá uma interface DB-API padrão síncrona, suporte de
primeira classe a pgvector via o pacote ``pgvector``, e não fecha a porta
pra async depois (psycopg3, ao contrário do psycopg2, também suporta
``async``/``await`` nativamente se um pipeline de ingestão concorrente vier
a precisar).

Connection string sempre vem de variável de ambiente (``DATABASE_URL``),
nunca hardcoded — mesma regra do projeto inteiro (ver ``.env.example``). Ao
contrário do lado C# (que precisa de um conversor manual pra passar do
formato URI pro formato chave=valor do Npgsql — ver
``NeonConnectionStringConverter`` em Forja.Infrastructure), psycopg aceita
o formato ``postgresql://usuario:senha@host/banco?...`` do Neon nativamente,
sem nenhuma conversão.
"""

from __future__ import annotations

import os
from collections.abc import Iterator
from contextlib import contextmanager
from datetime import datetime
from enum import Enum
from typing import Any
from uuid import UUID

import psycopg
from pgvector.psycopg import register_vector
from psycopg.types.json import Jsonb


class TipoFonte(str, Enum):
    """Espelha o enum Postgres ``tipo_fonte`` (ver ``schema-atual.sql``)."""

    LEI = "lei"
    EDITAL = "edital"
    PROVA = "prova"


class TipoQuestao(str, Enum):
    """Espelha o enum Postgres ``tipo_questao`` (ver ``schema-atual.sql``)."""

    CERTO_ERRADO = "certo_errado"
    MULTIPLA_ESCOLHA = "multipla_escolha"


class StatusQuestao(str, Enum):
    """Espelha o enum Postgres ``status_questao`` (ver ``schema-atual.sql``)."""

    RASCUNHO = "rascunho"
    EM_REVISAO = "em_revisao"
    APROVADA = "aprovada"
    REJEITADA = "rejeitada"


class OrigemQuestao(str, Enum):
    """Espelha o enum Postgres ``origem_questao`` (ver ``schema-atual.sql``)."""

    GERADA_IA = "gerada_ia"
    REPRODUZIDA_PROVA_OFICIAL = "reproduzida_prova_oficial"
    INEDITA = "inedita"


def obter_connection_string() -> str:
    """Lê a connection string do Postgres da variável de ambiente ``DATABASE_URL``.

    Raises:
        RuntimeError: Se a variável não estiver definida.
    """
    connection_string = os.environ.get("DATABASE_URL")
    if not connection_string:
        raise RuntimeError(
            "Variável de ambiente DATABASE_URL não definida — ver .env.example."
        )
    return connection_string


@contextmanager
def conectar() -> Iterator[psycopg.Connection[Any]]:
    """Abre uma conexão com o Postgres, com o tipo ``vector`` já registrado.

    Uso::

        with conectar() as conn:
            inserir_fonte_conteudo(conn, ...)
            conn.commit()

    Commit/rollback é responsabilidade de quem chama — este contexto só
    garante que a conexão é fechada ao sair do bloco (inclusive em caso de
    exceção), não decide quando persistir.
    """
    with psycopg.connect(obter_connection_string()) as conn:
        register_vector(conn)
        yield conn


def inserir_fonte_conteudo(
    conn: psycopg.Connection[Any],
    *,
    tipo: TipoFonte,
    titulo: str,
    texto_extraido: str,
    edital_id: UUID | None = None,
    caminho_arquivo: str | None = None,
    versao_em: datetime | None = None,
) -> UUID:
    """Insere uma linha em ``fontes_conteudo`` e devolve o ``id`` gerado.

    ``versao_em``, quando informado, é a versão *da fonte de origem* (ex.:
    data de modificação do arquivo no disco) — não a hora da inserção no
    banco. É o valor que ``obter_versao_em``/RN-014 compara depois pra
    decidir se reprocessa. Se omitido, o Postgres usa o default da coluna
    (``now()``, hora da inserção) — apropriado quando não existe uma noção
    de "versão da origem" separada (ex.: conteúdo colado à mão, sem arquivo).
    """
    if versao_em is not None:
        sql = """
            INSERT INTO fontes_conteudo (tipo, edital_id, titulo, caminho_arquivo, texto_extraido, versao_em)
            VALUES (%s, %s, %s, %s, %s, %s)
            RETURNING id
        """
        parametros: tuple[object, ...] = (tipo.value, edital_id, titulo, caminho_arquivo, texto_extraido, versao_em)
    else:
        sql = """
            INSERT INTO fontes_conteudo (tipo, edital_id, titulo, caminho_arquivo, texto_extraido)
            VALUES (%s, %s, %s, %s, %s)
            RETURNING id
        """
        parametros = (tipo.value, edital_id, titulo, caminho_arquivo, texto_extraido)

    with conn.cursor() as cur:
        cur.execute(sql, parametros)
        linha = cur.fetchone()
        assert linha is not None  # RETURNING de um INSERT bem-sucedido sempre devolve uma linha
        return UUID(str(linha[0]))


def inserir_chunk_conteudo(
    conn: psycopg.Connection[Any],
    *,
    fonte_id: UUID,
    texto: str,
    embedding: list[float],
    topico_id: UUID | None = None,
) -> UUID:
    """Insere uma linha em ``chunks_conteudo`` (incluindo o vetor) e devolve o ``id`` gerado.

    ``embedding`` precisa ter exatamente 1024 posições — a coluna é
    ``vector(1024)`` (dimensão do modelo voyage-3, ver
    ``infrastructure.embeddings``); o Postgres rejeita qualquer outro
    tamanho.
    """
    with conn.cursor() as cur:
        cur.execute(
            """
            INSERT INTO chunks_conteudo (fonte_id, topico_id, texto, embedding)
            VALUES (%s, %s, %s, %s)
            RETURNING id
            """,
            (fonte_id, topico_id, texto, embedding),
        )
        linha = cur.fetchone()
        assert linha is not None
        return UUID(str(linha[0]))


def obter_versao_em(conn: psycopg.Connection[Any], identificador: str) -> datetime | None:
    """Consulta a ``fontes_conteudo.versao_em`` mais recente para ``identificador``.

    ``identificador`` é o ``caminho_arquivo`` da fonte — o identificador
    natural de "já processei este arquivo antes?" no pipeline de ingestão,
    disponível antes mesmo de a linha existir. Não é o ``id`` (UUID
    interno), que só existe depois do primeiro insert.

    Usado pela RN-014 (incremental, Fase 3): antes de reprocessar um
    arquivo, compara a versão conhecida aqui com a versão atual do arquivo
    de origem, pra decidir se pula ou reembeda.

    Returns:
        A ``versao_em`` mais recente entre as fontes com esse
        ``caminho_arquivo``, ou ``None`` se o identificador nunca foi visto.
    """
    with conn.cursor() as cur:
        cur.execute(
            """
            SELECT versao_em
            FROM fontes_conteudo
            WHERE caminho_arquivo = %s
            ORDER BY versao_em DESC
            LIMIT 1
            """,
            (identificador,),
        )
        linha = cur.fetchone()
        result: datetime | None = linha[0] if linha is not None else None
        return result


def listar_disciplinas(conn: psycopg.Connection[Any]) -> dict[str, UUID]:
    """Lê o catálogo de disciplinas já cadastradas, como um mapa nome -> id.

    Usado pelo pipeline de digitalização de prova pra restringir a
    classificação do LLM (``infrastructure.llm.extrair_questoes_estruturadas``)
    a disciplinas que já existem — nunca inventar uma nova.
    """
    with conn.cursor() as cur:
        cur.execute("SELECT nome, id FROM disciplinas")
        return {nome: UUID(str(id_)) for nome, id_ in cur.fetchall()}


def existem_questoes_para_edital(conn: psycopg.Connection[Any], edital_id: UUID) -> bool:
    """Verifica se já existem questões gravadas para o ``edital_id`` de uma prova.

    Usado pela idempotência do pipeline de digitalização de prova: ``questoes``
    não tem coluna ``edital_id`` própria — a citação chega até o edital via
    ``fonte_prova_id -> fontes_conteudo.edital_id`` (a mesma fonte de conteúdo
    tipo ``prova`` criada por :func:`inserir_fonte_conteudo` na digitalização).
    """
    with conn.cursor() as cur:
        cur.execute(
            """
            SELECT count(*)
            FROM questoes q
            JOIN fontes_conteudo f ON f.id = q.fonte_prova_id
            WHERE f.edital_id = %s
            """,
            (edital_id,),
        )
        linha = cur.fetchone()
        return linha is not None and linha[0] > 0


def inserir_questao(
    conn: psycopg.Connection[Any],
    *,
    carreira_id: UUID,
    banca_id: UUID | None,
    disciplina_id: UUID,
    tipo: TipoQuestao,
    enunciado: str,
    alternativas: dict[str, str] | None,
    gabarito: str,
    explicacao: str,
    status: StatusQuestao,
    origem: OrigemQuestao,
    fonte_prova_id: UUID | None,
    ano: int | None,
    fontes_chunk_ids: list[UUID],
) -> UUID:
    """Insere uma linha em ``questoes`` e devolve o ``id`` gerado.

    ``alternativas`` precisa ser ``None`` quando ``tipo`` é
    ``TipoQuestao.CERTO_ERRADO`` e um mapa letra -> texto não vazio quando é
    ``TipoQuestao.MULTIPLA_ESCOLHA`` — o Postgres rejeita qualquer outra
    combinação (``chk_alternativas_multipla``); esta função não valida isso
    de novo, só passa adiante o que quem chama já decidiu.
    """
    with conn.cursor() as cur:
        cur.execute(
            """
            INSERT INTO questoes (
                carreira_id, banca_id, disciplina_id, tipo, enunciado, alternativas,
                gabarito, explicacao, fontes_chunk_ids, status, origem, fonte_prova_id, ano
            )
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
            RETURNING id
            """,
            (
                carreira_id,
                banca_id,
                disciplina_id,
                tipo.value,
                enunciado,
                Jsonb(alternativas) if alternativas is not None else None,
                gabarito,
                explicacao,
                fontes_chunk_ids,
                status.value,
                origem.value,
                fonte_prova_id,
                ano,
            ),
        )
        linha = cur.fetchone()
        assert linha is not None
        return UUID(str(linha[0]))
