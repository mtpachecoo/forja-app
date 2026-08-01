"""Pipeline de ingestão: PDF → texto → chunks → embeddings → Postgres.

Primeiro lugar do projeto onde ``core`` (parsing/chunking, puro, testado
isoladamente na Fase 1) e ``infrastructure`` (Postgres/Voyage, testados
isoladamente na Fase 2) rodam juntos.

Fluxo:
    1. Lê os bytes do PDF (I/O, aqui — não em ``core``).
    2. ``core.parsing.extrair_texto_pdf`` → texto.
    3. ``core.chunking.dividir_em_chunks`` → lista de :class:`~motor_conteudo.core.chunking.Chunk`.
    4. Pra cada chunk: ``infrastructure.embeddings.gerar_embedding`` (voyage-3,
       input_type="document").
    5. Grava a fonte e os chunks no Postgres, numa única transação — ou tudo
       ou nada.

Idempotência (RN-014): antes do passo 1, compara a versão já registrada
(``infrastructure.postgres.obter_versao_em``) com a versão atual do arquivo
(data de modificação no disco). Se a registrada já é igual ou mais recente,
pula o reprocessamento inteiro — e loga isso explicitamente (não é uma
skip silenciosa).
"""

from __future__ import annotations

import logging
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from uuid import UUID

from motor_conteudo.core.chunking import dividir_em_chunks
from motor_conteudo.infrastructure.embeddings import gerar_embedding
from motor_conteudo.infrastructure.postgres import (
    TipoFonte,
    conectar,
    inserir_chunk_conteudo,
    inserir_fonte_conteudo,
    obter_versao_em,
)
from motor_conteudo.pipelines.leitura_pdf import ler_pdf_ou_falhar

logger = logging.getLogger(__name__)


class ErroDeIngestao(Exception):
    """Erro no pipeline de ingestão.

    A mensagem já é pensada pra ser mostrada direto ao usuário da CLI (ver
    ``motor_conteudo.cli``) — não é um traceback cru de biblioteca interna.
    """


@dataclass(frozen=True)
class ResultadoIngestao:
    """Resultado de uma chamada a :func:`ingerir_pdf`.

    Attributes:
        pulou: ``True`` se a ingestão foi pulada por já estar atualizada
            (RN-014) — nesse caso ``fonte_id`` é ``None`` e
            ``quantidade_chunks`` é 0, nada foi lido/gravado.
        fonte_id: ``id`` da linha gravada em ``fontes_conteudo``, ou
            ``None`` se pulou.
        quantidade_chunks: Quantos chunks foram gravados em
            ``chunks_conteudo``.
    """

    pulou: bool
    fonte_id: UUID | None
    quantidade_chunks: int


def ingerir_pdf(
    caminho_pdf: Path,
    *,
    tipo: TipoFonte,
    titulo: str | None = None,
    edital_id: UUID | None = None,
) -> ResultadoIngestao:
    """Extrai, chunka, embeda e grava um PDF de edital/lei/prova.

    Args:
        caminho_pdf: Caminho do PDF no disco. Também serve como
            identificador pra idempotência (RN-014) — chamar de novo com o
            *mesmo* caminho é o que permite detectar "já processei isso".
        tipo: Tipo da fonte (``lei``, ``edital`` ou ``prova``).
        titulo: Título da fonte. Se omitido, usa o nome do arquivo (sem
            extensão).
        edital_id: Edital ao qual a fonte pertence, quando aplicável.

    Returns:
        :class:`ResultadoIngestao` — ``pulou=True`` se RN-014 decidiu que
        não havia nada novo a processar.

    Raises:
        ErroDeIngestao: Arquivo não existe, PDF corrompido/ilegível, ou PDF
            sem nenhum conteúdo extraível (ex.: só imagens escaneadas, sem
            OCR) — em nenhum desses casos algo é gravado no Postgres.
    """
    if not caminho_pdf.exists():
        raise ErroDeIngestao(f"Arquivo não encontrado: {caminho_pdf}")

    identificador = str(caminho_pdf)
    versao_atual = datetime.fromtimestamp(caminho_pdf.stat().st_mtime, tz=timezone.utc)
    titulo_final = titulo if titulo is not None else caminho_pdf.stem

    with conectar() as conn:
        versao_registrada = obter_versao_em(conn, identificador)

        if versao_registrada is not None and versao_registrada >= versao_atual:
            logger.info(
                "RN-014: pulando ingestão de %s — versão já registrada (%s) é igual ou "
                "mais recente que a versão atual do arquivo (%s). Nada foi lido/reprocessado.",
                identificador,
                versao_registrada.isoformat(),
                versao_atual.isoformat(),
            )
            return ResultadoIngestao(pulou=True, fonte_id=None, quantidade_chunks=0)

        texto = ler_pdf_ou_falhar(
            caminho_pdf,
            tipo_erro=ErroDeIngestao,
            # "Vazio" pra este pipeline é chunking vazio, não texto vazio — um texto
            # só com linhas que parecem título (sem nenhum corpo) gera chunks=[]
            # mesmo não sendo whitespace puro (ver core.chunking). Isso recomputa
            # dividir_em_chunks quando o texto não é vazio, mas é barato (regex puro).
            checagem_vazio=lambda texto_extraido: not dividir_em_chunks(texto_extraido),
        )
        chunks = dividir_em_chunks(texto)

        fonte_id = inserir_fonte_conteudo(
            conn,
            tipo=tipo,
            titulo=titulo_final,
            texto_extraido=texto,
            edital_id=edital_id,
            caminho_arquivo=identificador,
            versao_em=versao_atual,
        )

        for chunk in chunks:
            embedding = gerar_embedding(chunk.texto)
            inserir_chunk_conteudo(conn, fonte_id=fonte_id, texto=chunk.texto, embedding=embedding)

        conn.commit()

    logger.info(
        "Ingestão concluída: %s — %d chunk(s) gravado(s) (fonte %s).",
        identificador,
        len(chunks),
        fonte_id,
    )
    return ResultadoIngestao(pulou=False, fonte_id=fonte_id, quantidade_chunks=len(chunks))
