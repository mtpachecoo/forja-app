"""Carrega motor-conteudo/.env pros testes de pipeline (precisam de Postgres/Voyage reais)."""

from __future__ import annotations

from pathlib import Path

import pytest
from dotenv import load_dotenv
from fpdf import FPDF

load_dotenv(Path(__file__).parent.parent.parent / ".env")


@pytest.fixture
def caminho_pdf_temporario(tmp_path: Path) -> Path:
    """Cópia da fixture real da Fase 1 (lei 8.112, 6 chunks) num caminho único por teste.

    Um caminho único (via ``tmp_path``, exclusivo de cada teste) evita
    colisão de identificador entre testes diferentes — ``caminho_arquivo``
    é a chave que a idempotência (RN-014) usa pra saber "já vi isso antes",
    então dois testes reaproveitando o mesmo caminho literal se atrapalhariam.
    """
    origem = Path(__file__).parent.parent / "fixtures" / "lei_8112_arts_1_a_5.pdf"
    destino = tmp_path / "lei_8112_arts_1_a_5.pdf"
    destino.write_bytes(origem.read_bytes())
    return destino


@pytest.fixture
def caminho_pdf_minimo(tmp_path: Path) -> Path:
    """PDF com um único chunk — usado nos testes que ingerem mais de uma vez.

    A rota de sucesso chama a API real da Voyage uma vez por chunk; usar a
    fixture completa (6 chunks) nos testes de idempotência/reingestão, que
    ingerem duas vezes cada, multiplicaria as chamadas à API sem agregar
    nada ao que está sendo testado (o comportamento de pular/reprocessar,
    não a extração/chunking em si — já coberto por ``caminho_pdf_temporario``).
    """
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font("helvetica", size=11)
    pdf.multi_cell(0, 6, "Art. 1o Texto minimo so pra exercitar o pipeline de ponta a ponta.")
    destino = tmp_path / "minimo.pdf"
    destino.write_bytes(bytes(pdf.output()))
    return destino
