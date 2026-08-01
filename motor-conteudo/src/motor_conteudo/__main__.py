"""Permite `python -m motor_conteudo ...`."""

from __future__ import annotations

import sys

from motor_conteudo.cli import main

if __name__ == "__main__":
    sys.exit(main())
