"""CLI mínima do Motor de Conteúdo.

Uso::

    python -m motor_conteudo ingerir-edital caminho.pdf --tipo lei
    python -m motor_conteudo ingerir-edital caminho.pdf --tipo edital --titulo "Edital 1/2026"
    python -m motor_conteudo digitalizar-prova prova.pdf gabarito.pdf \\
        --carreira-id <uuid> --banca-id <uuid> --edital-id <uuid> --ano 2026
"""

from __future__ import annotations

import argparse
import logging
import sys
from pathlib import Path
from uuid import UUID

from dotenv import load_dotenv

from motor_conteudo.infrastructure.postgres import TipoFonte
from motor_conteudo.pipelines.digitalizar_prova import ErroDeDigitalizacao, digitalizar_prova
from motor_conteudo.pipelines.ingestao_rag import ErroDeIngestao, ingerir_pdf


def main(argv: list[str] | None = None) -> int:
    """Ponto de entrada da CLI. Devolve o código de saída do processo."""
    load_dotenv()
    logging.basicConfig(level=logging.INFO, format="%(levelname)s %(name)s: %(message)s")

    parser = _construir_parser()
    args = parser.parse_args(argv)

    if args.comando == "ingerir-edital":
        return _comando_ingerir_edital(args)
    if args.comando == "digitalizar-prova":
        return _comando_digitalizar_prova(args)

    parser.print_help()
    return 1


def _construir_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="python -m motor_conteudo")
    subparsers = parser.add_subparsers(dest="comando", required=True)

    ingerir = subparsers.add_parser(
        "ingerir-edital",
        help="Extrai, chunka, embeda e grava um PDF de edital/lei/prova no Postgres.",
    )
    ingerir.add_argument("caminho", type=Path, help="Caminho do arquivo PDF.")
    ingerir.add_argument(
        "--tipo",
        required=True,
        choices=[tipo.value for tipo in TipoFonte],
        help="Tipo da fonte de conteúdo.",
    )
    ingerir.add_argument(
        "--titulo",
        default=None,
        help="Título da fonte (default: nome do arquivo, sem extensão).",
    )

    digitalizar = subparsers.add_parser(
        "digitalizar-prova",
        help=(
            "Extrai questões estruturadas de uma prova oficial (PDF) + gabarito via LLM e "
            "grava em questoes com status em_revisao."
        ),
    )
    digitalizar.add_argument("prova", type=Path, help="Caminho do PDF da prova aplicada.")
    digitalizar.add_argument(
        "gabarito",
        type=Path,
        nargs="?",
        default=None,
        help="Caminho do PDF do gabarito oficial (omitir se vier no mesmo PDF da prova).",
    )
    digitalizar.add_argument(
        "--carreira-id", required=True, type=UUID, help="UUID da carreira à qual a prova se refere."
    )
    digitalizar.add_argument(
        "--banca-id", required=True, type=UUID, help="UUID da banca organizadora da prova."
    )
    digitalizar.add_argument(
        "--edital-id", required=True, type=UUID, help="UUID do edital ao qual a prova se refere."
    )
    digitalizar.add_argument("--ano", required=True, type=int, help="Ano de aplicação da prova.")

    return parser


def _comando_ingerir_edital(args: argparse.Namespace) -> int:
    try:
        resultado = ingerir_pdf(args.caminho, tipo=TipoFonte(args.tipo), titulo=args.titulo)
    except ErroDeIngestao as erro:
        print(f"Erro: {erro}", file=sys.stderr)
        return 1

    if resultado.pulou:
        print(f"Pulado — {args.caminho} já estava atualizado (RN-014).")
    else:
        print(f"Ingerido: {resultado.quantidade_chunks} chunk(s) gravado(s) (fonte {resultado.fonte_id}).")

    return 0


def _comando_digitalizar_prova(args: argparse.Namespace) -> int:
    try:
        resultado = digitalizar_prova(
            args.prova,
            args.gabarito,
            carreira_id=args.carreira_id,
            banca_id=args.banca_id,
            edital_id=args.edital_id,
            ano=args.ano,
        )
    except ErroDeDigitalizacao as erro:
        print(f"Erro: {erro}", file=sys.stderr)
        return 1

    if resultado.pulou:
        print(f"Pulado — já existem questões gravadas para o edital {args.edital_id} (idempotência).")
    else:
        print(
            f"Digitalização concluída: {resultado.quantidade_questoes} questão(ões) "
            f"gravada(s) em revisão (fonte {resultado.fonte_prova_id})."
        )

    return 0
