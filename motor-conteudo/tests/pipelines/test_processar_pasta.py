"""Testes de motor_conteudo.pipelines.processar_pasta — lote mockado, sem PDF/LLM/Postgres reais.

Tudo mockado: `ler_pdf_ou_falhar`, `classificar_documento`, `ingerir_pdf` e
`digitalizar_prova` nunca tocam um PDF/Gemini/Postgres de verdade — só os
arquivos precisam existir de fato (`iterdir`/pareamento por nome operam no
sistema de arquivos real, dentro de `tmp_path`). O foco é a lógica do lote em
si: pareamento prova+gabarito pelo nome, continuar processando os demais itens
quando um falha, e o relatório final bater com o que de fato foi gravado.
"""

from __future__ import annotations

from collections.abc import Callable, Iterator
from contextlib import contextmanager
from pathlib import Path
from unittest.mock import MagicMock, patch
from uuid import uuid4

import pytest

from motor_conteudo.infrastructure.llm import ClassificacaoDocumento
from motor_conteudo.pipelines.digitalizar_prova import ResultadoDigitalizacao
from motor_conteudo.pipelines.ingestao_rag import ResultadoIngestao
from motor_conteudo.pipelines.processar_pasta import ErroDeProcessamentoEmLote, processar_pasta

_CARREIRA_ID = uuid4()
_BANCA_ID = uuid4()
_EDITAL_ID = uuid4()


@contextmanager
def _conexao_fake() -> Iterator[MagicMock]:
    yield MagicMock()


def _classificador(mapa: dict[str, ClassificacaoDocumento]) -> Callable[..., ClassificacaoDocumento]:
    def _classificar(texto_capa: str, **_kwargs: object) -> ClassificacaoDocumento:
        for stem, classificacao in mapa.items():
            if stem in texto_capa:
                return classificacao
        raise AssertionError(f"sem classificação mockada pro texto de teste: {texto_capa!r}")

    return _classificar


def _criar_pdf(destino: Path) -> None:
    destino.write_bytes(b"conteudo qualquer -- ler_pdf_ou_falhar e mockado neste teste, nao le de verdade")


@pytest.fixture(autouse=True)
def _catalogo_mockado(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr("motor_conteudo.pipelines.processar_pasta.conectar", _conexao_fake)
    monkeypatch.setattr(
        "motor_conteudo.pipelines.processar_pasta.listar_carreiras", lambda conn: {"Carreira A": _CARREIRA_ID}
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.processar_pasta.listar_bancas", lambda conn: {"Banca X": _BANCA_ID}
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.processar_pasta.buscar_edital_id",
        lambda conn, *, carreira_id, banca_id, ano: (
            _EDITAL_ID if (carreira_id, banca_id, ano) == (_CARREIRA_ID, _BANCA_ID, 2026) else None
        ),
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.processar_pasta.ler_pdf_ou_falhar",
        lambda caminho, **_kwargs: f"texto de {caminho.stem}",
    )


def test_pasta_inexistente_lanca_erro(tmp_path: Path) -> None:
    with pytest.raises(ErroDeProcessamentoEmLote, match="[Pp]asta não encontrada"):
        processar_pasta(tmp_path / "nao-existe")


def test_pasta_sem_nenhum_pdf_lanca_erro(tmp_path: Path) -> None:
    (tmp_path / "nao-e-pdf.txt").write_text("qualquer coisa")

    with pytest.raises(ErroDeProcessamentoEmLote, match="[Nn]enhum PDF encontrado"):
        processar_pasta(tmp_path)


def test_lote_continua_apos_falha_e_conta_processados_e_falhados(tmp_path: Path) -> None:
    _criar_pdf(tmp_path / "edital_2026.pdf")
    _criar_pdf(tmp_path / "prova_carreira_a.pdf")
    _criar_pdf(tmp_path / "gabarito_carreira_a.pdf")
    _criar_pdf(tmp_path / "prova_sem_carreira.pdf")
    _criar_pdf(tmp_path / "zzz_lei_8112.pdf")

    mapa_classificacao = {
        "edital_2026": ClassificacaoDocumento(tipo="edital", carreira=None, banca=None, ano=2026),
        "prova_carreira_a": ClassificacaoDocumento(tipo="prova", carreira="Carreira A", banca="Banca X", ano=2026),
        # carreira nao identificada -- sem confirmacao manual em lote, este item deve falhar.
        "prova_sem_carreira": ClassificacaoDocumento(tipo="prova", carreira=None, banca=None, ano=None),
        "zzz_lei_8112": ClassificacaoDocumento(tipo="lei", carreira=None, banca=None, ano=None),
    }
    resultado_ingestao = ResultadoIngestao(pulou=False, fonte_id=uuid4(), quantidade_chunks=3)
    resultado_prova = ResultadoDigitalizacao(pulou=False, fonte_prova_id=uuid4(), quantidade_questoes=7)

    with (
        patch(
            "motor_conteudo.pipelines.processar_pasta.classificar_documento",
            side_effect=_classificador(mapa_classificacao),
        ),
        patch(
            "motor_conteudo.pipelines.processar_pasta.ingerir_pdf", return_value=resultado_ingestao
        ) as ingerir_mockado,
        patch(
            "motor_conteudo.pipelines.processar_pasta.digitalizar_prova", return_value=resultado_prova
        ) as digitalizar_mockado,
    ):
        relatorio = processar_pasta(tmp_path)

    # 4 itens no relatorio: o gabarito pareado nao vira item proprio, so entra
    # como argumento da prova correspondente.
    assert len(relatorio.itens) == 4
    assert relatorio.quantidade_sucesso == 3
    assert relatorio.quantidade_falha == 1

    itens_por_nome = {item.arquivo.name: item for item in relatorio.itens}

    assert itens_por_nome["edital_2026.pdf"].sucesso is True
    assert itens_por_nome["prova_carreira_a.pdf"].sucesso is True
    assert itens_por_nome["zzz_lei_8112.pdf"].sucesso is True

    item_falho = itens_por_nome["prova_sem_carreira.pdf"]
    assert item_falho.sucesso is False
    assert "Carreira não identificada" in item_falho.mensagem

    # ingerir_pdf chamado pro edital e pra lei, nunca pra prova.
    assert ingerir_mockado.call_count == 2

    # digitalizar_prova chamado uma unica vez, com o gabarito pareado pelo nome.
    digitalizar_mockado.assert_called_once()
    caminho_prova_chamado, caminho_gabarito_chamado = digitalizar_mockado.call_args[0]
    kwargs = digitalizar_mockado.call_args.kwargs
    assert caminho_prova_chamado.name == "prova_carreira_a.pdf"
    assert caminho_gabarito_chamado.name == "gabarito_carreira_a.pdf"
    assert kwargs["carreira_id"] == _CARREIRA_ID
    assert kwargs["banca_id"] == _BANCA_ID
    assert kwargs["edital_id"] == _EDITAL_ID
    assert kwargs["ano"] == 2026


def test_gabarito_sem_prova_correspondente_vira_item_falhado(tmp_path: Path) -> None:
    _criar_pdf(tmp_path / "gabarito_orfao.pdf")
    _criar_pdf(tmp_path / "edital_2026.pdf")

    mapa_classificacao = {
        "edital_2026": ClassificacaoDocumento(tipo="edital", carreira=None, banca=None, ano=2026),
    }
    resultado_ingestao = ResultadoIngestao(pulou=False, fonte_id=uuid4(), quantidade_chunks=1)

    with (
        patch(
            "motor_conteudo.pipelines.processar_pasta.classificar_documento",
            side_effect=_classificador(mapa_classificacao),
        ),
        patch("motor_conteudo.pipelines.processar_pasta.ingerir_pdf", return_value=resultado_ingestao),
        patch("motor_conteudo.pipelines.processar_pasta.digitalizar_prova") as digitalizar_mockado,
    ):
        relatorio = processar_pasta(tmp_path)

    assert len(relatorio.itens) == 2
    itens_por_nome = {item.arquivo.name: item for item in relatorio.itens}

    orfao = itens_por_nome["gabarito_orfao.pdf"]
    assert orfao.sucesso is False
    assert orfao.classificacao is None
    assert "correspondente" in orfao.mensagem

    assert itens_por_nome["edital_2026.pdf"].sucesso is True
    digitalizar_mockado.assert_not_called()
