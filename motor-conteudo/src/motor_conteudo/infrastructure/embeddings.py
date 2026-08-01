"""Cliente Voyage AI — geração de embeddings.

Modelo **voyage-3** (não reabre essa decisão — mesmo modelo já usado pelo
lado C# em ``VoyageEmbeddingService``, e mesma dimensão gravada no schema:
``chunks_conteudo.embedding vector(1024)``).

API key sempre vem de variável de ambiente (``VOYAGE_API_KEY``), nunca
hardcoded — mesma regra do projeto inteiro.

Diferença deliberada em relação ao lado C#: ``VoyageEmbeddingService`` (C#)
usa ``input_type="query"`` porque embeda a pergunta do usuário em tempo de
busca (RAG/dúvidas). Este módulo embeda o **conteúdo a ser armazenado**
(chunks, na ingestão) — por isso ``input_type="document"``, o parâmetro que
a própria Voyage recomenda pra texto que vai ser indexado, não buscado. É a
mesma API/modelo, papel diferente na ponta.

Retry em 429 (não em outros erros): contas Voyage sem cartão de pagamento
cadastrado ficam limitadas a 3 RPM — um pipeline de ingestão chamando uma
vez por chunk esbarra nisso na prática, não só em teste de carga. A resposta
429 da Voyage não traz ``Retry-After``, então o backoff aqui é uma espera
fixa calibrada pro próprio limite (3 RPM = uma requisição a cada 20s),
tentando de novo algumas vezes antes de desistir com um erro claro.
"""

from __future__ import annotations

import logging
import os
import time

import requests

_URL_EMBEDDINGS = "https://api.voyageai.com/v1/embeddings"
_MODELO = "voyage-3"
_DIMENSOES_ESPERADAS = 1024
_MAX_TENTATIVAS = 5
_ESPERA_ENTRE_TENTATIVAS_SEGUNDOS = 21  # pouco mais que 60s/3 RPM

logger = logging.getLogger(__name__)


def obter_api_key() -> str:
    """Lê a API key da Voyage da variável de ambiente ``VOYAGE_API_KEY``.

    Raises:
        RuntimeError: Se a variável não estiver definida.
    """
    api_key = os.environ.get("VOYAGE_API_KEY")
    if not api_key:
        raise RuntimeError(
            "Variável de ambiente VOYAGE_API_KEY não definida — ver .env.example."
        )
    return api_key


def gerar_embedding(texto: str) -> list[float]:
    """Gera o embedding de ``texto`` via Voyage AI (modelo voyage-3, 1024 dimensões).

    Interface deliberadamente simples (texto -> vetor): autenticação,
    protocolo HTTP e retry em rate limit ficam escondidos aqui dentro, não
    vazam pro chamador.

    Raises:
        RuntimeError: Se ``VOYAGE_API_KEY`` não estiver definida, se a
            resposta da API não vier com o vetor esperado, ou se o rate
            limit (429) persistir depois de todas as tentativas.
        requests.HTTPError: Se a chamada HTTP falhar com qualquer erro que
            não seja 429 — não é mascarada aqui; decidir tratamento pra
            esses é responsabilidade de quem chama.
    """
    api_key = obter_api_key()

    for tentativa in range(1, _MAX_TENTATIVAS + 1):
        resposta = requests.post(
            _URL_EMBEDDINGS,
            headers={"Authorization": f"Bearer {api_key}"},
            json={"input": [texto], "model": _MODELO, "input_type": "document"},
            timeout=30,
        )

        if resposta.status_code == 429:
            if tentativa == _MAX_TENTATIVAS:
                raise RuntimeError(
                    f"Rate limit da Voyage (429) persistiu depois de {_MAX_TENTATIVAS} tentativas. "
                    "Conta sem cartão de pagamento cadastrado fica limitada a 3 RPM — "
                    "ver https://dashboard.voyageai.com/."
                )
            logger.warning(
                "Rate limit da Voyage (429) na tentativa %d/%d — aguardando %ds antes de tentar de novo.",
                tentativa,
                _MAX_TENTATIVAS,
                _ESPERA_ENTRE_TENTATIVAS_SEGUNDOS,
            )
            time.sleep(_ESPERA_ENTRE_TENTATIVAS_SEGUNDOS)
            continue

        resposta.raise_for_status()

        corpo = resposta.json()
        dados = corpo.get("data")
        if not dados:
            raise RuntimeError("Resposta vazia da API de embeddings da Voyage.")

        embedding: list[float] = dados[0]["embedding"]
        if len(embedding) != _DIMENSOES_ESPERADAS:
            raise RuntimeError(
                f"Embedding da Voyage veio com {len(embedding)} dimensões, "
                f"esperado {_DIMENSOES_ESPERADAS} (modelo {_MODELO})."
            )

        return embedding

    raise AssertionError("inatingível: o loop sempre retorna ou lança antes de terminar")
