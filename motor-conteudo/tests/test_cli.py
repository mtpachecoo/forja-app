"""Testes de motor_conteudo.cli — comando `processar`, tudo mockado.

Nenhuma chamada real acontece aqui: nem Gemini, nem Postgres, nem os pipelines de
ingestão/digitalização em si (isso já é coberto pelos testes de cada pipeline). O
objetivo é só o fluxo de classificação -> confirmação do comando `processar`:
classificação correta chama o pipeline certo, campo não identificado pede input
manual antes de prosseguir, e confirmação negativa não grava nada.
"""

from __future__ import annotations

from contextlib import contextmanager
from pathlib import Path
from typing import Iterator
from unittest.mock import MagicMock, patch
from uuid import UUID, uuid4

from motor_conteudo.cli import main
from motor_conteudo.infrastructure.llm import ClassificacaoDocumento
from motor_conteudo.pipelines.digitalizar_prova import ResultadoDigitalizacao
from motor_conteudo.pipelines.ingestao_rag import ResultadoIngestao


@contextmanager
def _conexao_falsa() -> Iterator[MagicMock]:
    yield MagicMock()


def _mocks_de_catalogo(carreiras: dict[str, UUID], bancas: dict[str, UUID]) -> dict[str, object]:
    return {
        "motor_conteudo.cli.conectar": _conexao_falsa,
        "motor_conteudo.cli.listar_carreiras": lambda conn: carreiras,
        "motor_conteudo.cli.listar_bancas": lambda conn: bancas,
    }


def test_processar_classificacao_correta_confirma_e_ingere_edital(tmp_path: Path) -> None:
    caminho = tmp_path / "edital.pdf"
    classificacao = ClassificacaoDocumento(tipo="edital", carreira=None, banca=None, ano=None)
    resultado_ingestao = ResultadoIngestao(pulou=False, fonte_id=uuid4(), quantidade_chunks=5)

    with (
        patch("motor_conteudo.cli.ler_pdf_ou_falhar", return_value="texto do edital"),
        patch("motor_conteudo.cli.conectar", _conexao_falsa),
        patch("motor_conteudo.cli.listar_carreiras", return_value={"Carreira X": uuid4()}),
        patch("motor_conteudo.cli.listar_bancas", return_value={"Banca Y": uuid4()}),
        patch("motor_conteudo.cli.classificar_documento", return_value=classificacao) as classificar_mockado,
        patch("motor_conteudo.cli.ingerir_pdf", return_value=resultado_ingestao) as ingerir_mockado,
        patch("motor_conteudo.cli.digitalizar_prova") as digitalizar_mockado,
        patch("builtins.input", side_effect=["s"]),
    ):
        codigo = main(["processar", str(caminho)])

    assert codigo == 0
    classificar_mockado.assert_called_once()
    ingerir_mockado.assert_called_once()
    _, kwargs = ingerir_mockado.call_args
    assert kwargs["tipo"].value == "edital"
    digitalizar_mockado.assert_not_called()


def test_processar_campo_nao_identificado_pede_input_manual_e_digitaliza_prova(tmp_path: Path) -> None:
    caminho_prova = tmp_path / "prova.pdf"
    carreira_id = uuid4()
    banca_id = uuid4()
    edital_id = uuid4()
    carreiras = {"Analista Judiciário": carreira_id}
    bancas = {"Cebraspe": banca_id}

    # carreira não identificada (None) — o comando deve perguntar; banca e ano já vieram.
    classificacao = ClassificacaoDocumento(tipo="prova", carreira=None, banca="Cebraspe", ano=2025)
    resultado_digitalizacao = ResultadoDigitalizacao(
        pulou=False, fonte_prova_id=uuid4(), quantidade_questoes=10
    )

    entradas_do_usuario = [
        "Analista Judiciário",  # carreira informada manualmente
        str(edital_id),  # edital (sempre manual, nunca classificado)
        "s",  # confirmação
    ]

    with (
        patch("motor_conteudo.cli.ler_pdf_ou_falhar", return_value="texto da prova"),
        patch("motor_conteudo.cli.conectar", _conexao_falsa),
        patch("motor_conteudo.cli.listar_carreiras", return_value=carreiras),
        patch("motor_conteudo.cli.listar_bancas", return_value=bancas),
        patch("motor_conteudo.cli.classificar_documento", return_value=classificacao),
        patch("motor_conteudo.cli.ingerir_pdf") as ingerir_mockado,
        patch("motor_conteudo.cli.digitalizar_prova", return_value=resultado_digitalizacao) as digitalizar_mockado,
        patch("builtins.input", side_effect=entradas_do_usuario),
    ):
        codigo = main(["processar", str(caminho_prova)])

    assert codigo == 0
    ingerir_mockado.assert_not_called()
    digitalizar_mockado.assert_called_once()
    _, kwargs = digitalizar_mockado.call_args
    assert kwargs["carreira_id"] == carreira_id
    assert kwargs["banca_id"] == banca_id
    assert kwargs["edital_id"] == edital_id
    assert kwargs["ano"] == 2025


def test_processar_confirmacao_negativa_nao_grava_nada(tmp_path: Path) -> None:
    caminho = tmp_path / "lei.pdf"
    classificacao = ClassificacaoDocumento(tipo="lei", carreira=None, banca=None, ano=None)

    with (
        patch("motor_conteudo.cli.ler_pdf_ou_falhar", return_value="texto da lei"),
        patch("motor_conteudo.cli.conectar", _conexao_falsa),
        patch("motor_conteudo.cli.listar_carreiras", return_value={"Carreira X": uuid4()}),
        patch("motor_conteudo.cli.listar_bancas", return_value={"Banca Y": uuid4()}),
        patch("motor_conteudo.cli.classificar_documento", return_value=classificacao),
        patch("motor_conteudo.cli.ingerir_pdf") as ingerir_mockado,
        patch("motor_conteudo.cli.digitalizar_prova") as digitalizar_mockado,
        patch("builtins.input", side_effect=["n"]),
    ):
        codigo = main(["processar", str(caminho)])

    assert codigo == 0
    ingerir_mockado.assert_not_called()
    digitalizar_mockado.assert_not_called()
