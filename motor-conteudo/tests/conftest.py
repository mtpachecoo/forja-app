"""Fixtures compartilhadas pelos testes de parsing/chunking."""

from __future__ import annotations

from pathlib import Path

import pytest

_DIRETORIO_FIXTURES = Path(__file__).parent / "fixtures"


@pytest.fixture
def bytes_lei_8112() -> bytes:
    """PDF real: Art. 1º a 5º da Lei nº 8.112/1990."""
    return (_DIRETORIO_FIXTURES / "lei_8112_arts_1_a_5.pdf").read_bytes()


@pytest.fixture
def bytes_edital_exemplo() -> bytes:
    """PDF representativo de um edital de concurso público (estrutura por tópico numerado)."""
    return (_DIRETORIO_FIXTURES / "edital_exemplo.pdf").read_bytes()
