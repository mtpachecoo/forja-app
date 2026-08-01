"""Testes de integração de motor_conteudo.infrastructure.postgres — Postgres real (Neon).

Isolamento: nada aqui importa de motor_conteudo.core.

Nenhum dado de teste fica persistido: cada teste que escreve termina com
rollback explícito — nunca commita no Neon compartilhado (a conexão do
psycopg comita sozinha ao sair do `with` sem exceção, então o rollback
precisa ser explícito, não é o comportamento padrão).
"""

from __future__ import annotations

import os
import uuid
from datetime import datetime

import pytest

from motor_conteudo.infrastructure.postgres import (
    TipoFonte,
    conectar,
    inserir_chunk_conteudo,
    inserir_fonte_conteudo,
    obter_versao_em,
)

pytestmark = pytest.mark.skipif(
    not os.environ.get("DATABASE_URL"),
    reason="DATABASE_URL não definida — teste de integração precisa de Postgres real.",
)


def _vetor_sintetico(dimensoes: int = 1024) -> list[float]:
    """Vetor determinístico só pra testar o round-trip do tipo vector — não é um embedding real."""
    return [0.001 * i for i in range(dimensoes)]


def test_insere_e_le_fonte_conteudo_de_volta() -> None:
    identificador_unico = f"teste-motor-conteudo/{uuid.uuid4()}.pdf"

    with conectar() as conn:
        try:
            fonte_id = inserir_fonte_conteudo(
                conn,
                tipo=TipoFonte.LEI,
                titulo="Fixture de teste de integração — não é conteúdo real",
                texto_extraido="Texto de teste, descartado no rollback.",
                caminho_arquivo=identificador_unico,
            )

            with conn.cursor() as cur:
                cur.execute(
                    "SELECT tipo, titulo, texto_extraido, caminho_arquivo, edital_id "
                    "FROM fontes_conteudo WHERE id = %s",
                    (fonte_id,),
                )
                linha = cur.fetchone()

            assert linha is not None
            assert linha[0] == TipoFonte.LEI.value
            assert linha[1] == "Fixture de teste de integração — não é conteúdo real"
            assert linha[2] == "Texto de teste, descartado no rollback."
            assert linha[3] == identificador_unico
            assert linha[4] is None  # edital_id não informado
        finally:
            conn.rollback()


def test_insere_e_le_chunk_conteudo_com_vetor_de_volta() -> None:
    with conectar() as conn:
        try:
            fonte_id = inserir_fonte_conteudo(
                conn,
                tipo=TipoFonte.EDITAL,
                titulo="Fixture pai do chunk de teste",
                texto_extraido="Texto da fonte pai, irrelevante pro teste.",
            )
            vetor = _vetor_sintetico()

            chunk_id = inserir_chunk_conteudo(
                conn,
                fonte_id=fonte_id,
                texto="Chunk de teste de integração.",
                embedding=vetor,
            )

            with conn.cursor() as cur:
                cur.execute(
                    "SELECT texto, embedding, topico_id FROM chunks_conteudo WHERE id = %s",
                    (chunk_id,),
                )
                linha = cur.fetchone()

            assert linha is not None
            assert linha[0] == "Chunk de teste de integração."
            assert linha[2] is None  # topico_id não informado

            # embedding volta como pgvector.Vector (adapter registrado na conexão) — confirma
            # que o Postgres aceitou e devolveu o tipo vector corretamente, dimensão intacta.
            embedding_lido = linha[1].to_list()
            assert len(embedding_lido) == 1024
            assert embedding_lido[:5] == pytest.approx(vetor[:5], abs=1e-6)
        finally:
            conn.rollback()


def test_inserir_chunk_com_dimensao_errada_e_rejeitado_pelo_postgres() -> None:
    with conectar() as conn:
        try:
            fonte_id = inserir_fonte_conteudo(
                conn,
                tipo=TipoFonte.PROVA,
                titulo="Fixture pai pro teste de dimensão errada",
                texto_extraido="...",
            )

            with pytest.raises(Exception, match="expected 1024 dimensions"):
                inserir_chunk_conteudo(
                    conn,
                    fonte_id=fonte_id,
                    texto="Não deveria ser aceito.",
                    embedding=_vetor_sintetico(dimensoes=10),
                )
        finally:
            conn.rollback()


def test_obter_versao_em_identificador_desconhecido_retorna_none() -> None:
    with conectar() as conn:
        resultado = obter_versao_em(conn, f"nunca-existiu/{uuid.uuid4()}.pdf")

    assert resultado is None


def test_obter_versao_em_retorna_versao_apos_insercao() -> None:
    identificador_unico = f"teste-versao/{uuid.uuid4()}.pdf"

    with conectar() as conn:
        try:
            inserir_fonte_conteudo(
                conn,
                tipo=TipoFonte.PROVA,
                titulo="Fixture pra checar versao_em",
                texto_extraido="...",
                caminho_arquivo=identificador_unico,
            )

            versao = obter_versao_em(conn, identificador_unico)

            assert versao is not None
            assert isinstance(versao, datetime)
            assert versao.tzinfo is not None, "versao_em é timestamptz — precisa vir com timezone"
        finally:
            conn.rollback()
