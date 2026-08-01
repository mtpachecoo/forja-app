"""Testes de motor_conteudo.infrastructure.llm — Gemini mockado, sem gastar cota de API.

Isolamento: nada aqui importa de motor_conteudo.core ou motor_conteudo.pipelines. Diferente
de test_embeddings.py (API real da Voyage), aqui a chamada HTTP é sempre mockada — o
objetivo é testar o parsing do JSON estruturado, o retry em 429 e a disciplina de
grounding do prompt, não a API do Gemini em si (isso é responsabilidade do teste de
integração real do pipeline, gated por GEMINI_API_KEY).
"""

from __future__ import annotations

import json
from typing import Any
from unittest.mock import MagicMock, patch

import pytest

from motor_conteudo.infrastructure.llm import (
    _INSTRUCAO_DE_SISTEMA,
    _INSTRUCAO_DE_SISTEMA_CLASSIFICACAO,
    classificar_documento,
    extrair_questoes_estruturadas,
    obter_api_key,
)


def _resposta_gemini_ok(payload: dict[str, Any]) -> MagicMock:
    resposta = MagicMock()
    resposta.status_code = 200
    resposta.json.return_value = {
        "candidates": [{"content": {"parts": [{"text": json.dumps(payload)}]}}]
    }
    resposta.raise_for_status.return_value = None
    return resposta


def _resposta_gemini_429(retry_delay: str | None) -> MagicMock:
    resposta = MagicMock()
    resposta.status_code = 429
    detalhes = (
        [{"@type": "type.googleapis.com/google.rpc.RetryInfo", "retryDelay": retry_delay}]
        if retry_delay is not None
        else []
    )
    resposta.json.return_value = {"error": {"details": detalhes}}
    return resposta


@pytest.fixture(autouse=True)
def _gemini_api_key(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("GEMINI_API_KEY", "chave-de-teste-nao-e-real")


def test_obter_api_key_ausente_da_mensagem_clara(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)

    with pytest.raises(RuntimeError, match="GEMINI_API_KEY"):
        obter_api_key()


def test_extrai_questoes_estruturadas_faz_parsing_correto_do_json() -> None:
    payload = {
        "questoes": [
            {
                "numero": 1,
                "disciplina": "Direito Administrativo",
                "tipo": "multipla_escolha",
                "enunciado": "Assinale a alternativa correta sobre atos administrativos.",
                "alternativas": [
                    {"letra": "A", "texto": "Primeira alternativa."},
                    {"letra": "B", "texto": "Segunda alternativa."},
                ],
                "gabarito": "B",
            },
            {
                "numero": 2,
                "disciplina": "Português",
                "tipo": "certo_errado",
                "enunciado": "O período composto tem mais de uma oração.",
                "gabarito": "Certo",
            },
        ]
    }

    with patch("motor_conteudo.infrastructure.llm.requests.post", return_value=_resposta_gemini_ok(payload)):
        questoes = extrair_questoes_estruturadas(
            "texto da prova",
            "texto do gabarito",
            disciplinas_permitidas=["Direito Administrativo", "Português"],
        )

    assert len(questoes) == 2

    primeira = questoes[0]
    assert primeira.numero == 1
    assert primeira.disciplina == "Direito Administrativo"
    assert primeira.tipo == "multipla_escolha"
    assert primeira.alternativas == {"A": "Primeira alternativa.", "B": "Segunda alternativa."}
    assert primeira.gabarito == "B"

    segunda = questoes[1]
    assert segunda.tipo == "certo_errado"
    assert segunda.alternativas is None
    assert segunda.gabarito == "Certo"


def test_extrai_questoes_estruturadas_com_lista_vazia_de_questoes() -> None:
    with patch(
        "motor_conteudo.infrastructure.llm.requests.post",
        return_value=_resposta_gemini_ok({"questoes": []}),
    ):
        questoes = extrair_questoes_estruturadas(
            "texto ilegível", "texto ilegível", disciplinas_permitidas=["Português"]
        )

    assert questoes == []


def test_disciplinas_permitidas_vazia_nao_chama_a_api() -> None:
    with patch("motor_conteudo.infrastructure.llm.requests.post") as post_mockado:
        with pytest.raises(RuntimeError, match="[Dd]isciplinas permitidas"):
            extrair_questoes_estruturadas("texto", "texto", disciplinas_permitidas=[])

    post_mockado.assert_not_called()


def test_retry_em_429_usa_retry_delay_da_resposta_e_tenta_de_novo(monkeypatch: pytest.MonkeyPatch) -> None:
    esperas_registradas: list[float] = []
    monkeypatch.setattr("motor_conteudo.infrastructure.llm.time.sleep", esperas_registradas.append)

    payload_sucesso = {
        "questoes": [
            {
                "numero": 1,
                "disciplina": "Português",
                "tipo": "certo_errado",
                "enunciado": "Enunciado qualquer.",
                "gabarito": "Errado",
            }
        ]
    }
    respostas = [_resposta_gemini_429("12s"), _resposta_gemini_ok(payload_sucesso)]

    with patch("motor_conteudo.infrastructure.llm.requests.post", side_effect=respostas):
        questoes = extrair_questoes_estruturadas(
            "texto", "texto", disciplinas_permitidas=["Português"]
        )

    assert len(questoes) == 1
    assert esperas_registradas == [12]


def test_retry_em_429_esgotado_da_mensagem_clara(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setattr("motor_conteudo.infrastructure.llm.time.sleep", lambda _segundos: None)

    with patch(
        "motor_conteudo.infrastructure.llm.requests.post",
        return_value=_resposta_gemini_429(None),
    ):
        with pytest.raises(RuntimeError, match="[Rr]ate limit"):
            extrair_questoes_estruturadas("texto", "texto", disciplinas_permitidas=["Português"])


def test_prompt_e_instrucao_de_sistema_exigem_grounding_explicito() -> None:
    """Mesma disciplina do RagService (C#): responder ESTRITAMENTE com base no texto
    fornecido, sem completar com conhecimento próprio, e sem adivinhar em vez de omitir."""
    instrucao_normalizada = _INSTRUCAO_DE_SISTEMA.lower()

    assert "estritamente" in instrucao_normalizada
    assert "nunca criar" in instrucao_normalizada
    assert "inventar" in instrucao_normalizada
    assert "conhecimento próprio" in instrucao_normalizada
    assert "omita" in instrucao_normalizada


def test_requisicao_enviada_ao_gemini_inclui_a_instrucao_de_grounding() -> None:
    payload: dict[str, Any] = {"questoes": []}

    with patch(
        "motor_conteudo.infrastructure.llm.requests.post", return_value=_resposta_gemini_ok(payload)
    ) as post_mockado:
        extrair_questoes_estruturadas("texto da prova", "texto do gabarito", disciplinas_permitidas=["Geral"])

    _url, kwargs = post_mockado.call_args
    corpo_enviado = kwargs["json"]
    assert corpo_enviado["system_instruction"]["parts"][0]["text"] == _INSTRUCAO_DE_SISTEMA
    assert corpo_enviado["generationConfig"]["responseMimeType"] == "application/json"
    assert corpo_enviado["generationConfig"]["responseSchema"]["properties"]["questoes"]["items"][
        "properties"
    ]["disciplina"]["enum"] == ["Geral"]


def test_classificar_documento_com_todos_os_campos_identificados() -> None:
    payload = {"tipo": "prova", "carreira": "Analista Judiciário", "banca": "Cebraspe", "ano": 2025}

    with patch("motor_conteudo.infrastructure.llm.requests.post", return_value=_resposta_gemini_ok(payload)):
        classificacao = classificar_documento(
            "texto da capa",
            carreiras_permitidas=["Analista Judiciário"],
            bancas_permitidas=["Cebraspe"],
        )

    assert classificacao.tipo == "prova"
    assert classificacao.carreira == "Analista Judiciário"
    assert classificacao.banca == "Cebraspe"
    assert classificacao.ano == 2025


def test_classificar_documento_com_campos_nao_identificados_devolve_none() -> None:
    payload = {"tipo": "edital", "carreira": None, "banca": None, "ano": None}

    with patch("motor_conteudo.infrastructure.llm.requests.post", return_value=_resposta_gemini_ok(payload)):
        classificacao = classificar_documento(
            "texto da capa", carreiras_permitidas=["Analista Judiciário"], bancas_permitidas=["Cebraspe"]
        )

    assert classificacao.tipo == "edital"
    assert classificacao.carreira is None
    assert classificacao.banca is None
    assert classificacao.ano is None


def test_classificar_documento_carreiras_permitidas_vazia_nao_chama_a_api() -> None:
    with patch("motor_conteudo.infrastructure.llm.requests.post") as post_mockado:
        with pytest.raises(RuntimeError, match="[Cc]arreiras permitidas"):
            classificar_documento("texto", carreiras_permitidas=[], bancas_permitidas=["Cebraspe"])

    post_mockado.assert_not_called()


def test_classificar_documento_bancas_permitidas_vazia_nao_chama_a_api() -> None:
    with patch("motor_conteudo.infrastructure.llm.requests.post") as post_mockado:
        with pytest.raises(RuntimeError, match="[Bb]ancas permitidas"):
            classificar_documento(
                "texto", carreiras_permitidas=["Analista Judiciário"], bancas_permitidas=[]
            )

    post_mockado.assert_not_called()


def test_classificar_documento_instrucao_de_sistema_exige_grounding_explicito() -> None:
    instrucao_normalizada = _INSTRUCAO_DE_SISTEMA_CLASSIFICACAO.lower()

    assert "estritamente" in instrucao_normalizada
    assert "inventar" in instrucao_normalizada
    assert "conhecimento próprio" in instrucao_normalizada
    assert "null" in instrucao_normalizada


def test_classificar_documento_requisicao_restringe_carreira_e_banca_ao_catalogo() -> None:
    payload = {"tipo": "lei", "carreira": None, "banca": None, "ano": None}

    with patch(
        "motor_conteudo.infrastructure.llm.requests.post", return_value=_resposta_gemini_ok(payload)
    ) as post_mockado:
        classificar_documento(
            "texto da capa",
            carreiras_permitidas=["Analista Judiciário"],
            bancas_permitidas=["Cebraspe", "FGV"],
        )

    _url, kwargs = post_mockado.call_args
    corpo_enviado = kwargs["json"]
    assert corpo_enviado["system_instruction"]["parts"][0]["text"] == _INSTRUCAO_DE_SISTEMA_CLASSIFICACAO
    schema = corpo_enviado["generationConfig"]["responseSchema"]
    assert schema["properties"]["tipo"]["enum"] == ["lei", "edital", "prova"]
    assert schema["properties"]["carreira"]["enum"] == ["Analista Judiciário"]
    assert schema["properties"]["carreira"]["nullable"] is True
    assert schema["properties"]["banca"]["enum"] == ["Cebraspe", "FGV"]
    assert schema["properties"]["banca"]["nullable"] is True
    assert schema["properties"]["ano"]["nullable"] is True
