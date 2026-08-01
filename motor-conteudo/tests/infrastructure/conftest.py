"""Carrega motor-conteudo/.env pros testes de integração desta pasta.

Escopo deliberadamente restrito a tests/infrastructure/ — os testes de
core/ (tests/core/) não têm esse conftest e não dependem de nenhuma
variável de ambiente (é a garantia de isolamento pedida: nenhum teste desta
fase depende do core, e nenhum teste do core depende desta fase).
"""

from __future__ import annotations

from pathlib import Path

from dotenv import load_dotenv

load_dotenv(Path(__file__).parent.parent.parent / ".env")
