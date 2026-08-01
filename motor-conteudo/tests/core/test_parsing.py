"""Testes de motor_conteudo.core.parsing."""

from __future__ import annotations

import pytest
from fpdf import FPDF

from motor_conteudo.core.parsing import extrair_texto_pdf


def test_extrai_texto_da_lei_preserva_conteudo_e_ordem(bytes_lei_8112: bytes) -> None:
    texto = extrair_texto_pdf(bytes_lei_8112)

    assert "LEI Nº 8.112" in texto
    assert "Art. 1º" in texto
    assert "Art. 5º" in texto
    # Ordem de leitura preservada: Art. 1º precisa aparecer antes do Art. 5º.
    assert texto.index("Art. 1º") < texto.index("Art. 5º")


def test_extrai_texto_do_edital_preserva_topicos_numerados(bytes_edital_exemplo: bytes) -> None:
    texto = extrair_texto_pdf(bytes_edital_exemplo)

    assert "1. DAS DISPOSIÇÕES PRELIMINARES" in texto
    assert "2. DOS REQUISITOS" in texto
    assert texto.index("1. DAS DISPOSIÇÕES") < texto.index("2. DOS REQUISITOS")


def test_pdf_sem_texto_extraivel_retorna_string_vazia() -> None:
    # PDF válido, só que sem nenhum conteúdo de texto na página (equivalente
    # ao caso real de PDF escaneado como imagem, sem OCR).
    pdf = FPDF()
    pdf.add_page()
    conteudo_pdf = bytes(pdf.output())

    assert extrair_texto_pdf(conteudo_pdf) == ""


def test_bytes_invalidos_nao_sao_mascarados_como_pdf_valido() -> None:
    # Núcleo puro não engole erro silenciosamente — bytes que não são um
    # PDF de verdade devem propagar uma exceção, não devolver "".
    with pytest.raises(Exception):  # noqa: B017 - tipo exato vem do pdfminer/pypdfium2, não é contrato nosso
        extrair_texto_pdf(b"isto nao e um pdf")
