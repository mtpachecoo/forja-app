"""Pipeline de digitalização: PDF de prova + gabarito → questões estruturadas → Postgres.

Diferente de ``pipelines.ingestao_rag`` (chunk de lei/edital pra embeddings de
RAG), aqui a unidade de saída é uma **questão estruturada** (enunciado,
alternativas, gabarito) — formato varia por banca, por isso passa por um LLM
(``infrastructure.llm.extrair_questoes_estruturadas``), sempre grounded no
texto real dos PDFs, nunca inventando além do que está escrito.

Fluxo:
    1. Lê os bytes dos PDFs (prova + gabarito, ou só a prova se os dois
       vierem no mesmo arquivo) e extrai o texto (``core.parsing``).
    2. Lê o catálogo de disciplinas já cadastradas
       (``infrastructure.postgres.listar_disciplinas``) — restringe a
       classificação do LLM a esse catálogo, nunca inventa disciplina nova.
    3. Chama o LLM pra estruturar questão por questão.
    4. Grava uma fonte de conteúdo tipo ``prova`` (texto integral, ligada ao
       ``edital_id`` recebido) e, pra cada questão, uma linha em ``questoes``
       com ``status='em_revisao'`` (nunca aprovada direto — mesma regra do
       backend) e ``origem='reproduzida_prova_oficial'``.

Citação completa (RN-015): ``questoes`` não tem coluna ``edital_id`` própria
— o vínculo até o edital/banca/ano da prova de origem passa por
``fonte_prova_id -> fontes_conteudo.edital_id``, mesmo padrão já usado pra
citar a origem de um chunk de RAG. ``carreira_id``, ``banca_id`` e ``ano`` são
gravados diretamente em cada questão, e chegam como parâmetro do pipeline —
não inferidos, quem roda o pipeline já sabe qual prova é.

Idempotência: antes de processar, verifica se já existem questões para esse
``edital_id`` (``infrastructure.postgres.existem_questoes_para_edital``) — se
sim, pula o pipeline inteiro (nenhum PDF é lido, nenhuma chamada ao LLM
acontece) e loga isso explicitamente, mesma disciplina da RN-014.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from uuid import UUID

import psycopg

from motor_conteudo.infrastructure.llm import QuestaoExtraida, extrair_questoes_estruturadas
from motor_conteudo.infrastructure.postgres import (
    OrigemQuestao,
    StatusQuestao,
    TipoFonte,
    TipoQuestao,
    conectar,
    existem_questoes_para_edital,
    inserir_fonte_conteudo,
    inserir_questao,
    listar_disciplinas,
)
from motor_conteudo.pipelines.leitura_pdf import ler_pdf_ou_falhar

logger = logging.getLogger(__name__)

_SEPARADOR_GABARITO = "\n\n===== GABARITO OFICIAL =====\n\n"


class ErroDeDigitalizacao(Exception):
    """Erro no pipeline de digitalização de prova.

    A mensagem já é pensada pra ser mostrada direto ao usuário da CLI (ver
    ``motor_conteudo.cli``) — não é um traceback cru de biblioteca interna.
    """


@dataclass(frozen=True)
class ResultadoDigitalizacao:
    """Resultado de uma chamada a :func:`digitalizar_prova`.

    Attributes:
        pulou: ``True`` se a digitalização foi pulada por já existirem
            questões gravadas para esse ``edital_id`` — nesse caso
            ``fonte_prova_id`` é ``None`` e ``quantidade_questoes`` é 0, nada
            foi lido/gravado.
        fonte_prova_id: ``id`` da linha gravada em ``fontes_conteudo``
            (tipo ``prova``), ou ``None`` se pulou.
        quantidade_questoes: Quantas questões foram gravadas em ``questoes``.
    """

    pulou: bool
    fonte_prova_id: UUID | None
    quantidade_questoes: int


def digitalizar_prova(
    caminho_prova: Path,
    caminho_gabarito: Path | None,
    *,
    carreira_id: UUID,
    banca_id: UUID | None,
    edital_id: UUID,
    ano: int,
) -> ResultadoDigitalizacao:
    """Extrai, estrutura via LLM e grava as questões de uma prova oficial.

    Args:
        caminho_prova: Caminho do PDF da prova aplicada.
        caminho_gabarito: Caminho do PDF do gabarito oficial, ou ``None`` se
            vier no mesmo arquivo da prova (nesse caso o texto da prova é
            usado também como texto do gabarito).
        carreira_id: Carreira à qual a prova se refere.
        banca_id: Banca organizadora da prova, quando aplicável.
        edital_id: Edital ao qual a prova se refere — também a chave da
            idempotência (ver módulo).
        ano: Ano de aplicação da prova.

    Returns:
        :class:`ResultadoDigitalizacao` — ``pulou=True`` se já havia questões
        gravadas para esse edital.

    Raises:
        ErroDeDigitalizacao: Arquivo não existe, PDF corrompido/ilegível, PDF
            sem conteúdo extraível, nenhuma disciplina cadastrada, o LLM não
            extraiu nenhuma questão, ou o LLM devolveu uma questão que não
            passa nas checagens de consistência (disciplina fora do
            catálogo, gabarito sem alternativa correspondente) — em nenhum
            desses casos algo é gravado no Postgres.
    """
    with conectar() as conn:
        if existem_questoes_para_edital(conn, edital_id):
            logger.info(
                "Pulando digitalização — já existem questões gravadas para o edital %s. "
                "Nenhum PDF foi lido, nenhuma chamada ao LLM foi feita.",
                edital_id,
            )
            return ResultadoDigitalizacao(pulou=True, fonte_prova_id=None, quantidade_questoes=0)

        texto_prova = _ler_texto_pdf(caminho_prova)

        if caminho_gabarito is None or caminho_gabarito == caminho_prova:
            texto_gabarito = texto_prova
            texto_fonte_completo = texto_prova
        else:
            texto_gabarito = _ler_texto_pdf(caminho_gabarito)
            texto_fonte_completo = f"{texto_prova}{_SEPARADOR_GABARITO}{texto_gabarito}"

        disciplinas_permitidas = listar_disciplinas(conn)
        if not disciplinas_permitidas:
            raise ErroDeDigitalizacao(
                "Nenhuma disciplina cadastrada no catálogo — cadastre ao menos uma disciplina "
                "antes de digitalizar uma prova."
            )

        try:
            questoes_extraidas = extrair_questoes_estruturadas(
                texto_prova,
                texto_gabarito,
                disciplinas_permitidas=list(disciplinas_permitidas.keys()),
            )
        except Exception as erro:
            raise ErroDeDigitalizacao(f"Falha ao extrair questões via LLM: {erro}") from erro

        if not questoes_extraidas:
            raise ErroDeDigitalizacao(
                f"O LLM não extraiu nenhuma questão de {caminho_prova} — nada foi gravado."
            )

        fonte_prova_id = inserir_fonte_conteudo(
            conn,
            tipo=TipoFonte.PROVA,
            titulo=f"Prova oficial — {caminho_prova.stem} (edital {edital_id})",
            texto_extraido=texto_fonte_completo,
            edital_id=edital_id,
            caminho_arquivo=str(caminho_prova),
        )

        for questao in questoes_extraidas:
            _gravar_questao(
                conn,
                questao,
                disciplinas_permitidas=disciplinas_permitidas,
                carreira_id=carreira_id,
                banca_id=banca_id,
                fonte_prova_id=fonte_prova_id,
                ano=ano,
            )

        conn.commit()

    logger.info(
        "Digitalização concluída: %s — %d questão(ões) gravada(s) em revisão (fonte %s).",
        caminho_prova,
        len(questoes_extraidas),
        fonte_prova_id,
    )
    return ResultadoDigitalizacao(
        pulou=False, fonte_prova_id=fonte_prova_id, quantidade_questoes=len(questoes_extraidas)
    )


def _ler_texto_pdf(caminho: Path) -> str:
    return ler_pdf_ou_falhar(
        caminho,
        tipo_erro=ErroDeDigitalizacao,
        checagem_vazio=lambda texto_extraido: not texto_extraido.strip(),
    )


def _gravar_questao(
    conn: psycopg.Connection[Any],
    questao: QuestaoExtraida,
    *,
    disciplinas_permitidas: dict[str, UUID],
    carreira_id: UUID,
    banca_id: UUID | None,
    fonte_prova_id: UUID,
    ano: int,
) -> None:
    """Valida a consistência de uma questão extraída e grava em ``questoes``.

    As checagens aqui são uma rede de segurança contra o LLM "inventar" algo
    fora do que foi extraído — não confiam cegamente na saída do modelo só
    porque o ``responseSchema`` já restringiu o formato: disciplina precisa
    bater com o catálogo real (não só com o que o modelo *disse* que
    escolheu), e o gabarito de uma questão de múltipla escolha precisa
    corresponder a uma das alternativas efetivamente extraídas.
    """
    disciplina_id = disciplinas_permitidas.get(questao.disciplina)
    if disciplina_id is None:
        raise ErroDeDigitalizacao(
            f"Questão nº {questao.numero}: o LLM classificou a disciplina como "
            f"{questao.disciplina!r}, que não está no catálogo cadastrado. Nada foi gravado."
        )

    try:
        tipo = TipoQuestao(questao.tipo)
    except ValueError as erro:
        raise ErroDeDigitalizacao(
            f"Questão nº {questao.numero}: tipo {questao.tipo!r} desconhecido."
        ) from erro

    if tipo is TipoQuestao.MULTIPLA_ESCOLHA:
        if not questao.alternativas:
            raise ErroDeDigitalizacao(
                f"Questão nº {questao.numero}: tipo multipla_escolha sem alternativas extraídas."
            )
        if questao.gabarito not in questao.alternativas:
            raise ErroDeDigitalizacao(
                f"Questão nº {questao.numero}: gabarito {questao.gabarito!r} não corresponde a "
                "nenhuma alternativa extraída — possível invenção do modelo, não gravado."
            )
    elif questao.alternativas:
        raise ErroDeDigitalizacao(
            f"Questão nº {questao.numero}: tipo certo_errado não deveria ter alternativas."
        )

    explicacao = (
        f"Questão nº {questao.numero} reproduzida de prova oficial — "
        "explicação pendente de revisão editorial."
    )

    inserir_questao(
        conn,
        carreira_id=carreira_id,
        banca_id=banca_id,
        disciplina_id=disciplina_id,
        tipo=tipo,
        enunciado=questao.enunciado,
        alternativas=questao.alternativas,
        gabarito=questao.gabarito,
        explicacao=explicacao,
        status=StatusQuestao.EM_REVISAO,
        origem=OrigemQuestao.REPRODUZIDA_PROVA_OFICIAL,
        fonte_prova_id=fonte_prova_id,
        ano=ano,
        fontes_chunk_ids=[],
    )
