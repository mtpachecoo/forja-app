"""Testes de motor_conteudo.pipelines.digitalizar_prova — ramos de erro, tudo mockado.

Diferente de test_digitalizar_prova.py (que precisa de Postgres real pra criar e
limpar linhas de apoio — carreira/banca/disciplina/edital), aqui Postgres e LLM
são sempre mockados: o objetivo é exercitar ramos de validação/erro do pipeline
que a suíte real (gated por DATABASE_URL) não cobre hoje — prova+gabarito em
arquivos separados, catálogo de disciplinas vazio, falha do LLM, lista de
questões vazia, tipo desconhecido, alternativas indevidas —, não o caminho de
gravação real em si (isso já é responsabilidade de test_digitalizar_prova.py).
"""

from __future__ import annotations

from collections.abc import Iterator
from contextlib import contextmanager
from pathlib import Path
from unittest.mock import MagicMock
from uuid import uuid4

import pytest
from fpdf import FPDF

from motor_conteudo.infrastructure.llm import QuestaoExtraida
from motor_conteudo.pipelines.digitalizar_prova import (
    ErroDeDigitalizacao,
    ResultadoDigitalizacao,
    digitalizar_prova,
)

_CARREIRA_ID = uuid4()
_BANCA_ID = uuid4()
_EDITAL_ID = uuid4()
_DISCIPLINA_NOME = "Direito Administrativo"
_DISCIPLINA_ID = uuid4()


def _pdf_com_texto(destino: Path, texto: str) -> None:
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font("helvetica", size=11)
    pdf.multi_cell(0, 6, texto)
    destino.write_bytes(bytes(pdf.output()))


@contextmanager
def _conexao_fake() -> Iterator[MagicMock]:
    yield MagicMock()


def _questao_valida(tipo: str = "certo_errado", alternativas: dict[str, str] | None = None) -> QuestaoExtraida:
    return QuestaoExtraida(
        numero=1,
        disciplina=_DISCIPLINA_NOME,
        tipo=tipo,
        enunciado="Enunciado qualquer, fixo pro teste.",
        alternativas=alternativas,
        gabarito="A" if tipo == "multipla_escolha" else "Certo",
    )


def _digitalizar(caminho_prova: Path, caminho_gabarito: Path | None = None) -> ResultadoDigitalizacao:
    return digitalizar_prova(
        caminho_prova,
        caminho_gabarito,
        carreira_id=_CARREIRA_ID,
        banca_id=_BANCA_ID,
        edital_id=_EDITAL_ID,
        ano=2026,
    )


@pytest.fixture(autouse=True)
def _postgres_mockado(monkeypatch: pytest.MonkeyPatch) -> None:
    """Mocka toda a camada Postgres do pipeline — nenhum teste deste arquivo toca o banco."""
    monkeypatch.setattr("motor_conteudo.pipelines.digitalizar_prova.conectar", _conexao_fake)
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.existem_questoes_para_edital",
        MagicMock(return_value=False),
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.listar_disciplinas",
        MagicMock(return_value={_DISCIPLINA_NOME: _DISCIPLINA_ID}),
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.inserir_fonte_conteudo",
        MagicMock(return_value=uuid4()),
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.inserir_questao",
        MagicMock(return_value=uuid4()),
    )


def test_prova_e_gabarito_em_arquivos_separados_usa_o_texto_de_cada_um(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    caminho_prova = tmp_path / "prova.pdf"
    caminho_gabarito = tmp_path / "gabarito.pdf"
    _pdf_com_texto(caminho_prova, "Texto exclusivo da prova aplicada.")
    _pdf_com_texto(caminho_gabarito, "Texto exclusivo do gabarito oficial.")

    chamada_llm = MagicMock(return_value=[_questao_valida()])
    monkeypatch.setattr("motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas", chamada_llm)
    fonte_mockada = MagicMock(return_value=uuid4())
    monkeypatch.setattr("motor_conteudo.pipelines.digitalizar_prova.inserir_fonte_conteudo", fonte_mockada)

    resultado = _digitalizar(caminho_prova, caminho_gabarito)

    assert not resultado.pulou
    assert resultado.quantidade_questoes == 1

    texto_prova_passado, texto_gabarito_passado = chamada_llm.call_args[0]
    assert "exclusivo da prova" in texto_prova_passado
    assert "exclusivo do gabarito" in texto_gabarito_passado
    assert texto_prova_passado != texto_gabarito_passado

    texto_fonte_gravado = fonte_mockada.call_args.kwargs["texto_extraido"]
    assert "exclusivo da prova" in texto_fonte_gravado
    assert "exclusivo do gabarito" in texto_fonte_gravado


def test_lista_de_disciplinas_cadastradas_vazia_nao_chama_llm(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.listar_disciplinas", MagicMock(return_value={})
    )
    chamada_llm = MagicMock()
    monkeypatch.setattr("motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas", chamada_llm)

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_com_texto(caminho_prova, "Texto qualquer da prova.")

    with pytest.raises(ErroDeDigitalizacao, match="[Nn]enhuma disciplina cadastrada"):
        _digitalizar(caminho_prova)

    chamada_llm.assert_not_called()


def test_excecao_do_llm_e_envolvida_em_erro_de_digitalizacao(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas",
        MagicMock(side_effect=RuntimeError("Rate limit do Gemini (429) persistiu depois de 5 tentativas.")),
    )
    caminho_prova = tmp_path / "prova.pdf"
    _pdf_com_texto(caminho_prova, "Texto qualquer da prova.")

    with pytest.raises(ErroDeDigitalizacao, match="Falha ao extrair questões via LLM"):
        _digitalizar(caminho_prova)


def test_lista_de_questoes_extraidas_vazia_nao_grava_nada(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas",
        MagicMock(return_value=[]),
    )
    fonte_mockada = MagicMock()
    monkeypatch.setattr("motor_conteudo.pipelines.digitalizar_prova.inserir_fonte_conteudo", fonte_mockada)

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_com_texto(caminho_prova, "Texto qualquer da prova.")

    with pytest.raises(ErroDeDigitalizacao, match="não extraiu nenhuma questão"):
        _digitalizar(caminho_prova)

    fonte_mockada.assert_not_called()


def test_tipo_de_questao_desconhecido_nao_grava_nada(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    questao_tipo_invalido = QuestaoExtraida(
        numero=1,
        disciplina=_DISCIPLINA_NOME,
        tipo="tipo_que_nao_existe",
        enunciado="Enunciado qualquer.",
        alternativas=None,
        gabarito="Certo",
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas",
        MagicMock(return_value=[questao_tipo_invalido]),
    )
    inserir_questao_mockada = MagicMock()
    monkeypatch.setattr("motor_conteudo.pipelines.digitalizar_prova.inserir_questao", inserir_questao_mockada)

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_com_texto(caminho_prova, "Texto qualquer da prova.")

    with pytest.raises(ErroDeDigitalizacao, match="desconhecido"):
        _digitalizar(caminho_prova)

    inserir_questao_mockada.assert_not_called()


def test_alternativas_indevidas_em_questao_certo_errado_nao_grava_nada(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    questao_com_alternativas_indevidas = QuestaoExtraida(
        numero=1,
        disciplina=_DISCIPLINA_NOME,
        tipo="certo_errado",
        enunciado="Enunciado qualquer.",
        alternativas={"A": "Não deveria existir para certo_errado."},
        gabarito="Certo",
    )
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas",
        MagicMock(return_value=[questao_com_alternativas_indevidas]),
    )
    inserir_questao_mockada = MagicMock()
    monkeypatch.setattr("motor_conteudo.pipelines.digitalizar_prova.inserir_questao", inserir_questao_mockada)

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_com_texto(caminho_prova, "Texto qualquer da prova.")

    with pytest.raises(ErroDeDigitalizacao, match="não deveria ter alternativas"):
        _digitalizar(caminho_prova)

    inserir_questao_mockada.assert_not_called()
