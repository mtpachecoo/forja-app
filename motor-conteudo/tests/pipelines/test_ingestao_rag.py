"""Testes de integração de motor_conteudo.pipelines.ingestao_rag.

Primeiro lugar da suíte onde core + infrastructure rodam juntos de verdade
(contra Postgres e Voyage reais) — as duas camadas já foram testadas
isoladamente nas fases anteriores.

Cada teste que grava dado limpa depois de si (DELETE explícito, não
rollback): idempotência (RN-014) depende de o dado persistir *entre*
chamadas dentro do mesmo teste, então não dá pra usar transação sem commit
como nos testes de infrastructure.
"""

from __future__ import annotations

import os
from pathlib import Path

import pytest
from fpdf import FPDF

from motor_conteudo.infrastructure.postgres import TipoFonte, conectar
from motor_conteudo.pipelines.ingestao_rag import ErroDeIngestao, ingerir_pdf

pytestmark = pytest.mark.skipif(
    not os.environ.get("DATABASE_URL") or not os.environ.get("VOYAGE_API_KEY"),
    reason="DATABASE_URL/VOYAGE_API_KEY não definidas — teste de integração precisa de Postgres+Voyage reais.",
)


def _limpar_fonte(identificador: str) -> None:
    """Remove qualquer fonte (e seus chunks, via FK ON DELETE CASCADE) com esse caminho_arquivo."""
    with conectar() as conn:
        with conn.cursor() as cur:
            cur.execute("DELETE FROM fontes_conteudo WHERE caminho_arquivo = %s", (identificador,))
        conn.commit()


def test_ingestao_completa_grava_chunks_com_embedding_de_1024_dimensoes(
    caminho_pdf_temporario: Path,
) -> None:
    identificador = str(caminho_pdf_temporario)
    try:
        resultado = ingerir_pdf(caminho_pdf_temporario, tipo=TipoFonte.LEI, titulo="Teste de ingestão — Fase 3")

        assert not resultado.pulou
        assert resultado.fonte_id is not None
        assert resultado.quantidade_chunks > 0

        with conectar() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT tipo, titulo, caminho_arquivo FROM fontes_conteudo WHERE id = %s",
                    (resultado.fonte_id,),
                )
                fonte = cur.fetchone()

                cur.execute(
                    "SELECT texto, embedding FROM chunks_conteudo WHERE fonte_id = %s ORDER BY criado_em",
                    (resultado.fonte_id,),
                )
                chunks_gravados = cur.fetchall()

        assert fonte is not None
        assert fonte[0] == TipoFonte.LEI.value
        assert fonte[2] == identificador

        assert len(chunks_gravados) == resultado.quantidade_chunks
        for texto_chunk, embedding in chunks_gravados:
            assert texto_chunk  # nenhum chunk vazio foi gravado
            assert len(embedding.to_list()) == 1024
    finally:
        _limpar_fonte(identificador)


def test_ingestao_repetida_pula_e_nao_duplica(caminho_pdf_minimo: Path) -> None:
    identificador = str(caminho_pdf_minimo)
    try:
        primeira_vez = ingerir_pdf(caminho_pdf_minimo, tipo=TipoFonte.LEI, titulo="Teste de idempotência")
        assert not primeira_vez.pulou

        segunda_vez = ingerir_pdf(caminho_pdf_minimo, tipo=TipoFonte.LEI, titulo="Teste de idempotência")

        assert segunda_vez.pulou is True
        assert segunda_vez.fonte_id is None
        assert segunda_vez.quantidade_chunks == 0

        # A garantia real de idempotência: só existe UMA fonte pra esse
        # identificador, e a mesma quantidade de chunks de antes — nada foi
        # duplicado nem reprocessado.
        with conectar() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT count(*) FROM fontes_conteudo WHERE caminho_arquivo = %s",
                    (identificador,),
                )
                total_fontes = cur.fetchone()
                assert total_fontes is not None
                assert total_fontes[0] == 1

                cur.execute(
                    "SELECT count(*) FROM chunks_conteudo WHERE fonte_id = %s",
                    (primeira_vez.fonte_id,),
                )
                total_chunks = cur.fetchone()
                assert total_chunks is not None
                assert total_chunks[0] == primeira_vez.quantidade_chunks
    finally:
        _limpar_fonte(identificador)


def test_reingestao_apos_arquivo_modificado_nao_pula(caminho_pdf_minimo: Path) -> None:
    identificador = str(caminho_pdf_minimo)
    try:
        primeira_vez = ingerir_pdf(caminho_pdf_minimo, tipo=TipoFonte.LEI, titulo="Teste de reingestão")
        assert not primeira_vez.pulou

        # Simula o arquivo tendo sido modificado depois da primeira ingestão
        # — mtime estritamente maior que o registrado.
        novo_mtime = caminho_pdf_minimo.stat().st_mtime + 5
        os.utime(caminho_pdf_minimo, (novo_mtime, novo_mtime))

        segunda_vez = ingerir_pdf(caminho_pdf_minimo, tipo=TipoFonte.LEI, titulo="Teste de reingestão")

        assert not segunda_vez.pulou, "arquivo modificado depois do último registro não deveria ser pulado"
        assert segunda_vez.fonte_id is not None
        assert segunda_vez.fonte_id != primeira_vez.fonte_id
    finally:
        _limpar_fonte(identificador)


def test_pdf_corrompido_nao_grava_nada_e_da_mensagem_clara(tmp_path: Path) -> None:
    caminho_invalido = tmp_path / "corrompido.pdf"
    caminho_invalido.write_bytes(b"isto definitivamente nao e um arquivo PDF valido")
    identificador = str(caminho_invalido)

    with pytest.raises(ErroDeIngestao, match="[Nn].o foi poss.vel ler o PDF"):
        ingerir_pdf(caminho_invalido, tipo=TipoFonte.LEI, titulo="Não deveria chegar a gravar nada")

    with conectar() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT count(*) FROM fontes_conteudo WHERE caminho_arquivo = %s", (identificador,))
            total = cur.fetchone()
            assert total is not None
            assert total[0] == 0


def test_pdf_sem_texto_extraivel_nao_grava_nada_e_da_mensagem_clara(tmp_path: Path) -> None:
    # PDF válido, mas sem nenhum conteúdo de texto (equivalente a um
    # escaneado sem OCR) — mesmo caso de borda já coberto em core/infra,
    # agora verificando que o pipeline também não engole isso silenciosamente.
    pdf = FPDF()
    pdf.add_page()
    caminho_vazio = tmp_path / "vazio.pdf"
    caminho_vazio.write_bytes(bytes(pdf.output()))
    identificador = str(caminho_vazio)

    with pytest.raises(ErroDeIngestao, match="[Nn]enhum conte.do extra.vel"):
        ingerir_pdf(caminho_vazio, tipo=TipoFonte.EDITAL, titulo="PDF vazio, não deveria gravar nada")

    with conectar() as conn:
        with conn.cursor() as cur:
            cur.execute("SELECT count(*) FROM fontes_conteudo WHERE caminho_arquivo = %s", (identificador,))
            total = cur.fetchone()
            assert total is not None
            assert total[0] == 0


def test_arquivo_inexistente_da_mensagem_clara_sem_tocar_no_banco(tmp_path: Path) -> None:
    caminho_inexistente = tmp_path / "nao-existe.pdf"

    with pytest.raises(ErroDeIngestao, match="Arquivo n.o encontrado"):
        ingerir_pdf(caminho_inexistente, tipo=TipoFonte.PROVA, titulo="Nunca deveria chegar no Postgres")
