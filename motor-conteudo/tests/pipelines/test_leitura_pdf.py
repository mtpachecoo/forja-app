"""Testes de motor_conteudo.pipelines.leitura_pdf — compartilhado pelos dois pipelines.

Tudo aqui é offline: nenhum Postgres, Voyage ou Gemini envolvido. Os testes de
``ingerir_pdf``/``digitalizar_prova`` confirmam que o comportamento por
pipeline não mudou com a extração; este arquivo testa o helper em si,
isolado de ambos.
"""

from __future__ import annotations

from pathlib import Path

import pytest
from fpdf import FPDF

from motor_conteudo.pipelines.leitura_pdf import ler_pdf_ou_falhar


class _ErroDeTeste(Exception):
    """Exceção qualquer, só pra confirmar que ``tipo_erro`` é respeitado."""


def _pdf_com_texto(destino: Path, texto: str) -> None:
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font("helvetica", size=11)
    pdf.multi_cell(0, 6, texto)
    destino.write_bytes(bytes(pdf.output()))


def test_arquivo_inexistente_lanca_tipo_erro_informado(tmp_path: Path) -> None:
    caminho = tmp_path / "nao-existe.pdf"

    with pytest.raises(_ErroDeTeste, match="Arquivo não encontrado"):
        ler_pdf_ou_falhar(caminho, tipo_erro=_ErroDeTeste, checagem_vazio=lambda texto: not texto)


def test_pdf_corrompido_lanca_tipo_erro_informado(tmp_path: Path) -> None:
    caminho = tmp_path / "corrompido.pdf"
    caminho.write_bytes(b"isto definitivamente nao e um arquivo PDF valido")

    with pytest.raises(_ErroDeTeste, match="[Nn].o foi poss.vel ler o PDF"):
        ler_pdf_ou_falhar(caminho, tipo_erro=_ErroDeTeste, checagem_vazio=lambda texto: not texto)


def test_checagem_vazio_true_lanca_tipo_erro_informado(tmp_path: Path) -> None:
    caminho = tmp_path / "prova.pdf"
    _pdf_com_texto(caminho, "Qualquer texto, a checagem abaixo sempre considera vazio.")

    with pytest.raises(_ErroDeTeste, match="[Nn]enhum conte.do extra.vel"):
        ler_pdf_ou_falhar(caminho, tipo_erro=_ErroDeTeste, checagem_vazio=lambda texto: True)


def test_checagem_vazio_false_devolve_o_texto_extraido(tmp_path: Path) -> None:
    caminho = tmp_path / "prova.pdf"
    _pdf_com_texto(caminho, "Texto de verdade, deve ser devolvido intacto.")

    texto = ler_pdf_ou_falhar(caminho, tipo_erro=_ErroDeTeste, checagem_vazio=lambda texto: False)

    assert "Texto de verdade" in texto
