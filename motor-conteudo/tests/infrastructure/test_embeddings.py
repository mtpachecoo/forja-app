"""Testes de motor_conteudo.infrastructure.embeddings — Voyage.

Dois grupos, mesmo padrão de tests/infrastructure/test_llm.py:

- Mockado (maioria): retry em 429 até esgotar as tentativas, resposta sem os
  dados esperados e embedding com dimensão errada — sem gastar cota de API.
- Integração real (gated por VOYAGE_API_KEY): confirma a API de verdade.

Isolamento: nada aqui importa de motor_conteudo.core.
"""

from __future__ import annotations

import os
from unittest.mock import MagicMock, patch

import pytest

from motor_conteudo.infrastructure.embeddings import gerar_embedding, obter_api_key


def _resposta_voyage_ok(embedding: list[float]) -> MagicMock:
    resposta = MagicMock()
    resposta.status_code = 200
    resposta.json.return_value = {"data": [{"embedding": embedding}]}
    resposta.raise_for_status.return_value = None
    return resposta


def _resposta_voyage_429() -> MagicMock:
    resposta = MagicMock()
    resposta.status_code = 429
    return resposta


def test_obter_api_key_ausente_da_mensagem_clara(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("VOYAGE_API_KEY", raising=False)

    with pytest.raises(RuntimeError, match="VOYAGE_API_KEY"):
        obter_api_key()


def test_retry_em_429_esgotado_da_mensagem_clara(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("VOYAGE_API_KEY", "chave-de-teste-nao-e-real")
    monkeypatch.setattr("motor_conteudo.infrastructure.embeddings.time.sleep", lambda _segundos: None)

    with patch(
        "motor_conteudo.infrastructure.embeddings.requests.post",
        return_value=_resposta_voyage_429(),
    ) as post_mockado:
        with pytest.raises(RuntimeError, match="[Rr]ate limit"):
            gerar_embedding("texto qualquer")

    assert post_mockado.call_count == 5  # _MAX_TENTATIVAS


def test_resposta_sem_dados_esperados_da_erro_claro(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("VOYAGE_API_KEY", "chave-de-teste-nao-e-real")
    resposta = MagicMock()
    resposta.status_code = 200
    resposta.json.return_value = {}
    resposta.raise_for_status.return_value = None

    with patch("motor_conteudo.infrastructure.embeddings.requests.post", return_value=resposta):
        with pytest.raises(RuntimeError, match="[Rr]esposta vazia"):
            gerar_embedding("texto qualquer")


def test_embedding_com_dimensao_errada_da_erro_claro(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("VOYAGE_API_KEY", "chave-de-teste-nao-e-real")

    with patch(
        "motor_conteudo.infrastructure.embeddings.requests.post",
        return_value=_resposta_voyage_ok([0.1] * 10),
    ):
        with pytest.raises(RuntimeError, match="1024"):
            gerar_embedding("texto qualquer")


@pytest.mark.skipif(
    not os.environ.get("VOYAGE_API_KEY"),
    reason="VOYAGE_API_KEY não definida — teste de integração precisa da API real da Voyage.",
)
def test_gerar_embedding_de_texto_real_tem_1024_dimensoes() -> None:
    embedding = gerar_embedding(
        "Art. 5º São requisitos básicos para investidura em cargo público."
    )

    assert len(embedding) == 1024
    assert all(isinstance(valor, float) for valor in embedding)
    # Não é um vetor de zeros/degenerado — confirma que veio uma resposta de verdade da API.
    assert any(valor != 0.0 for valor in embedding)


@pytest.mark.skipif(
    not os.environ.get("VOYAGE_API_KEY"),
    reason="VOYAGE_API_KEY não definida — teste de integração precisa da API real da Voyage.",
)
def test_textos_diferentes_geram_embeddings_diferentes() -> None:
    embedding_a = gerar_embedding("Art. 1º Esta Lei institui o Regime Jurídico.")
    embedding_b = gerar_embedding("1. DAS DISPOSIÇÕES PRELIMINARES do edital.")

    assert embedding_a != embedding_b
