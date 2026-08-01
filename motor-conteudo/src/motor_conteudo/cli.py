"""CLI mínima do Motor de Conteúdo.

Uso::

    python -m motor_conteudo ingerir-edital caminho.pdf --tipo lei
    python -m motor_conteudo ingerir-edital caminho.pdf --tipo edital --titulo "Edital 1/2026"
    python -m motor_conteudo digitalizar-prova prova.pdf gabarito.pdf \\
        --carreira-id <uuid> --banca-id <uuid> --edital-id <uuid> --ano 2026
    python -m motor_conteudo processar caminho.pdf [gabarito.pdf]
"""

from __future__ import annotations

import argparse
import logging
import sys
from pathlib import Path
from uuid import UUID

from dotenv import load_dotenv

from motor_conteudo.infrastructure.llm import ClassificacaoDocumento, classificar_documento
from motor_conteudo.infrastructure.postgres import TipoFonte, conectar, listar_bancas, listar_carreiras
from motor_conteudo.pipelines.digitalizar_prova import ErroDeDigitalizacao, digitalizar_prova
from motor_conteudo.pipelines.ingestao_rag import ErroDeIngestao, ingerir_pdf
from motor_conteudo.pipelines.leitura_pdf import ler_pdf_ou_falhar

_TAMANHO_TEXTO_CAPA = 6000  # caracteres — cobre capa/rosto sem exigir o PDF inteiro


class ErroDeProcessamento(Exception):
    """Erro no comando ``processar``.

    A mensagem já é pensada pra ser mostrada direto ao usuário da CLI — não é
    um traceback cru de biblioteca interna.
    """


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
    if args.comando == "processar":
        return _comando_processar(args)

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

    processar = subparsers.add_parser(
        "processar",
        help=(
            "Classifica o tipo/metadado de um PDF via Gemini, pede confirmação e então chama "
            "o pipeline correspondente (ingerir-edital ou digitalizar-prova)."
        ),
    )
    processar.add_argument("caminho", type=Path, help="Caminho do PDF a classificar.")
    processar.add_argument(
        "gabarito",
        type=Path,
        nargs="?",
        default=None,
        help="Caminho do PDF do gabarito oficial, se o documento classificado for uma prova.",
    )

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


def _comando_processar(args: argparse.Namespace) -> int:
    try:
        texto = ler_pdf_ou_falhar(
            args.caminho,
            tipo_erro=ErroDeProcessamento,
            checagem_vazio=lambda texto_extraido: not texto_extraido.strip(),
        )

        with conectar() as conn:
            carreiras = listar_carreiras(conn)
            bancas = listar_bancas(conn)

        if not carreiras or not bancas:
            raise ErroDeProcessamento(
                "Catálogo de carreiras/bancas vazio — cadastre ao menos uma de cada antes de "
                "processar um documento."
            )

        try:
            classificacao = classificar_documento(
                texto[:_TAMANHO_TEXTO_CAPA],
                carreiras_permitidas=list(carreiras),
                bancas_permitidas=list(bancas),
            )
        except Exception as erro:
            raise ErroDeProcessamento(f"Falha ao classificar documento via Gemini: {erro}") from erro

        print("Classificação do Gemini:")
        print(f"  tipo: {classificacao.tipo}")
        print(f"  carreira: {classificacao.carreira or '(não identificada)'}")
        print(f"  banca: {classificacao.banca or '(não identificada)'}")
        print(f"  ano: {classificacao.ano if classificacao.ano is not None else '(não identificado)'}")

        if classificacao.tipo == "prova":
            return _confirmar_e_processar_prova(args, classificacao, carreiras, bancas)
        return _confirmar_e_processar_edital(args, classificacao)
    except ErroDeProcessamento as erro:
        print(f"Erro: {erro}", file=sys.stderr)
        return 1


def _confirmar_e_processar_edital(args: argparse.Namespace, classificacao: ClassificacaoDocumento) -> int:
    resposta = input(f"Confirma ingestão como '{classificacao.tipo}'? (s/n): ").strip().lower()
    if resposta not in ("s", "sim"):
        print("Cancelado — nada foi gravado.")
        return 0

    try:
        resultado = ingerir_pdf(args.caminho, tipo=TipoFonte(classificacao.tipo))
    except ErroDeIngestao as erro:
        print(f"Erro: {erro}", file=sys.stderr)
        return 1

    if resultado.pulou:
        print(f"Pulado — {args.caminho} já estava atualizado (RN-014).")
    else:
        print(f"Ingerido: {resultado.quantidade_chunks} chunk(s) gravado(s) (fonte {resultado.fonte_id}).")

    return 0


def _confirmar_e_processar_prova(
    args: argparse.Namespace,
    classificacao: ClassificacaoDocumento,
    carreiras: dict[str, UUID],
    bancas: dict[str, UUID],
) -> int:
    carreira_nome = classificacao.carreira
    if carreira_nome is None:
        carreira_nome = input("Carreira não identificada — informe o nome exato do catálogo: ").strip()
    if carreira_nome not in carreiras:
        raise ErroDeProcessamento(f"Carreira {carreira_nome!r} não está no catálogo cadastrado.")

    banca_nome = classificacao.banca
    if banca_nome is None:
        banca_nome = (
            input(
                "Banca não identificada — informe o nome exato do catálogo "
                "(ou deixe em branco se não houver banca): "
            ).strip()
            or None
        )
    if banca_nome is not None and banca_nome not in bancas:
        raise ErroDeProcessamento(f"Banca {banca_nome!r} não está no catálogo cadastrado.")

    ano = classificacao.ano
    if ano is None:
        ano_texto = input("Ano não identificado — informe o ano: ").strip()
        try:
            ano = int(ano_texto)
        except ValueError as erro:
            raise ErroDeProcessamento(f"Ano informado ({ano_texto!r}) não é um número.") from erro

    edital_texto = input("Edital (UUID) ao qual este documento se refere: ").strip()
    try:
        edital_id = UUID(edital_texto)
    except ValueError as erro:
        raise ErroDeProcessamento(f"Edital informado ({edital_texto!r}) não é um UUID válido.") from erro

    print(
        f"Prova será gravada como: carreira={carreira_nome!r}, banca={banca_nome or '(nenhuma)'!r}, "
        f"ano={ano}, edital={edital_id}."
    )
    resposta = input("Confirma digitalização da prova? (s/n): ").strip().lower()
    if resposta not in ("s", "sim"):
        print("Cancelado — nada foi gravado.")
        return 0

    try:
        resultado = digitalizar_prova(
            args.caminho,
            args.gabarito,
            carreira_id=carreiras[carreira_nome],
            banca_id=bancas[banca_nome] if banca_nome is not None else None,
            edital_id=edital_id,
            ano=ano,
        )
    except ErroDeDigitalizacao as erro:
        print(f"Erro: {erro}", file=sys.stderr)
        return 1

    if resultado.pulou:
        print(f"Pulado — já existem questões gravadas para o edital {edital_id} (idempotência).")
    else:
        print(
            f"Digitalização concluída: {resultado.quantidade_questoes} questão(ões) "
            f"gravada(s) em revisão (fonte {resultado.fonte_prova_id})."
        )

    return 0
