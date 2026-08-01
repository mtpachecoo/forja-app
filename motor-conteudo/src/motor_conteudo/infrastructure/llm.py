"""Cliente Gemini — extração estruturada de questões e classificação de metadado de PDF.

Modelo **gemini-2.5-flash** (não reabre essa decisão — mesmo provedor/modelo já
em uso no lado C# para RAG de dúvidas contextuais, ver
``Forja.Infrastructure.Ia/DependencyInjection.cs`` e ``Gemini__Modelo`` no
``.env`` do backend).

API key sempre vem de variável de ambiente (``GEMINI_API_KEY``), nunca
hardcoded — mesma regra do projeto inteiro.

Disciplina de grounding (mesma já usada no ``RagService`` em C#, ver
``InstrucaoDeSistema`` em ``Forja.Application/Duvidas/RagService.cs``): o
prompt exige que o modelo se baseie ESTRITAMENTE no texto fornecido, sem
completar com conhecimento próprio — aqui adaptado para digitalização de
prova (não inventar enunciado/alternativa/gabarito, e omitir a questão em vez
de adivinhar quando o texto estiver incompleto/ilegível).

Saída estruturada via ``responseMimeType: application/json`` +
``responseSchema`` do próprio Gemini (não regex/markdown fencing sobre a
resposta) — inclui uma restrição de enum nas disciplinas permitidas, lidas do
catálogo já cadastrado no Postgres (``infrastructure.postgres.listar_disciplinas``),
para o modelo também não inventar uma disciplina fora do catálogo existente.
``classificar_documento`` aplica a mesma disciplina de grounding e a mesma restrição
de enum de catálogo (``listar_carreiras``/``listar_bancas``) para classificar tipo,
carreira, banca e ano de um PDF — mas, ao contrário da digitalização de prova,
carreira/banca/ano podem legitimamente vir ``None`` quando o texto não os identifica.

Retry em 429: o free tier do gemini-2.5-flash é limitado a 10 RPM (ver
https://ai.google.dev/gemini-api/docs/rate-limits) — um pipeline chamando uma
vez por prova esbarra nisso na prática se rodar em sequência rápida com
outras chamadas. Diferente da Voyage, a resposta 429 do Gemini normalmente
traz um ``RetryInfo.retryDelay`` explícito no corpo do erro; o backoff aqui
usa esse valor quando presente, e cai para uma espera fixa calibrada pro
próprio limite (10 RPM = uma requisição a cada 6s) só quando o campo não vem.
"""

from __future__ import annotations

import json
import logging
import os
import time
from dataclasses import dataclass
from typing import Any

import requests

_URL_GENERATE_CONTENT = "https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent"
_MODELO = "gemini-2.5-flash"
_MAX_TENTATIVAS = 5
_ESPERA_FALLBACK_SEGUNDOS = 7  # pouco mais que 60s/10 RPM (limite do free tier)

_INSTRUCAO_DE_SISTEMA = (
    "Você é um assistente de digitalização de provas de concurso público. Sua única tarefa é "
    "EXTRAIR questões que já existem no texto fornecido — nunca criar, completar, corrigir, "
    "traduzir ou reformular enunciado, alternativas ou gabarito. Baseie-se ESTRITAMENTE no texto "
    "da prova e do gabarito fornecidos no prompt do usuário. Não use conhecimento próprio para "
    "inventar ou completar informação ausente ou ilegível. Se uma questão estiver incompleta, "
    "corrompida pela extração do PDF, ou sem gabarito correspondente claro no texto fornecido, "
    "OMITA essa questão do resultado em vez de adivinhar."
)

logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class QuestaoExtraida:
    """Uma questão extraída pelo Gemini a partir do texto da prova + gabarito.

    Attributes:
        numero: Número da questão, como aparece na prova — usado só para
            rastreabilidade/log (não existe coluna correspondente em
            ``questoes``), literalmente observado no texto, não inventado.
        disciplina: Nome de uma disciplina do catálogo passado em
            ``disciplinas_permitidas`` — nunca um nome fora dessa lista
            (imposto via ``responseSchema``, ver :func:`extrair_questoes_estruturadas`).
        tipo: ``"certo_errado"`` ou ``"multipla_escolha"``.
        enunciado: Enunciado completo da questão.
        alternativas: Mapa letra -> texto (``{"A": "...", "B": "..."}``),
            só quando ``tipo == "multipla_escolha"``; ``None`` caso contrário.
        gabarito: Resposta correta segundo o gabarito oficial fornecido.
    """

    numero: int
    disciplina: str
    tipo: str
    enunciado: str
    alternativas: dict[str, str] | None
    gabarito: str


_INSTRUCAO_DE_SISTEMA_CLASSIFICACAO = (
    "Você é um assistente de classificação de metadado de documentos de concurso público. Sua "
    "única tarefa é IDENTIFICAR o tipo do documento (lei, edital ou prova) e, quando o texto "
    "deixar explícito, a carreira, a banca organizadora e o ano a que ele se refere. Baseie-se "
    "ESTRITAMENTE no texto das primeiras páginas fornecido no prompt do usuário — não use "
    "conhecimento próprio para inventar ou adivinhar carreira, banca ou ano. Se não conseguir "
    "identificar um desses três campos com confiança a partir do texto fornecido, devolva null "
    "para ele em vez de chutar."
)


@dataclass(frozen=True)
class ClassificacaoDocumento:
    """Classificação de metadado de um PDF, extraída pelo Gemini a partir do texto das primeiras
    páginas (ver :func:`classificar_documento`).

    Attributes:
        tipo: ``"lei"``, ``"edital"`` ou ``"prova"``.
        carreira: Nome de uma carreira do catálogo passado em
            ``carreiras_permitidas``, ou ``None`` se o texto não identificar a
            carreira com confiança — nunca um nome fora desse catálogo
            (imposto via ``responseSchema``).
        banca: Nome de uma banca do catálogo passado em ``bancas_permitidas``,
            ou ``None`` nas mesmas condições de ``carreira``.
        ano: Ano do documento, ou ``None`` se não identificado no texto.
    """

    tipo: str
    carreira: str | None
    banca: str | None
    ano: int | None


def obter_api_key() -> str:
    """Lê a API key do Gemini da variável de ambiente ``GEMINI_API_KEY``.

    Raises:
        RuntimeError: Se a variável não estiver definida.
    """
    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key:
        raise RuntimeError(
            "Variável de ambiente GEMINI_API_KEY não definida — ver .env.example."
        )
    return api_key


def extrair_questoes_estruturadas(
    texto_prova: str,
    texto_gabarito: str,
    *,
    disciplinas_permitidas: list[str],
) -> list[QuestaoExtraida]:
    """Extrai questões estruturadas do texto de uma prova + gabarito via Gemini.

    Interface deliberadamente simples (texto -> lista de questões):
    autenticação, protocolo HTTP, retry em rate limit e o parsing do JSON
    estruturado ficam escondidos aqui dentro, não vazam pro chamador.

    Args:
        texto_prova: Texto extraído do PDF da prova aplicada.
        texto_gabarito: Texto extraído do PDF do gabarito oficial (pode ser
            igual a ``texto_prova`` quando os dois vêm no mesmo arquivo).
        disciplinas_permitidas: Nomes das disciplinas já cadastradas no
            catálogo (``infrastructure.postgres.listar_disciplinas``) — o
            modelo é restrito a escolher uma destas por questão, nunca a
            inventar uma disciplina nova.

    Returns:
        Lista de :class:`QuestaoExtraida`, na ordem devolvida pelo modelo.
        Pode ser vazia se o modelo não reconhecer nenhuma questão completa
        no texto fornecido.

    Raises:
        RuntimeError: Se ``disciplinas_permitidas`` estiver vazia, se
            ``GEMINI_API_KEY`` não estiver definida, se a resposta não vier
            no formato esperado, ou se o rate limit (429) persistir depois
            de todas as tentativas.
        requests.HTTPError: Se a chamada HTTP falhar com qualquer erro que
            não seja 429 — não é mascarada aqui; decidir tratamento pra
            esses é responsabilidade de quem chama.
    """
    if not disciplinas_permitidas:
        raise RuntimeError(
            "Lista de disciplinas permitidas está vazia — nada pra restringir a "
            "classificação do modelo. Cadastre ao menos uma disciplina antes de digitalizar."
        )

    payload = _chamar_gemini(
        instrucao_de_sistema=_INSTRUCAO_DE_SISTEMA,
        prompt=_construir_prompt(texto_prova, texto_gabarito, disciplinas_permitidas),
        schema=_construir_schema(disciplinas_permitidas),
    )
    return _parsear_questoes(payload)


def classificar_documento(
    texto_capa: str,
    *,
    carreiras_permitidas: list[str],
    bancas_permitidas: list[str],
) -> ClassificacaoDocumento:
    """Classifica o tipo e os metadados de um PDF a partir do texto das primeiras páginas.

    Igual à disciplina de grounding de :func:`extrair_questoes_estruturadas`: carreira e banca
    são restritas por ``responseSchema`` ao catálogo já cadastrado no Postgres
    (``infrastructure.postgres.listar_carreiras``/``listar_bancas``) — o modelo nunca inventa um
    nome fora dele. Diferente da extração de questões, aqui ``carreira``/``banca``/``ano`` também
    podem legitimamente vir ``None``: nem todo documento identifica os três já na capa, e o
    modelo é instruído a devolver ``None`` em vez de adivinhar — decidir se isso exige
    preenchimento manual antes de prosseguir é responsabilidade de quem chama (ver
    ``motor_conteudo.cli``), não deste cliente.

    Args:
        texto_capa: Texto das primeiras páginas do PDF (capa/rosto), de onde o tipo/metadado do
            documento normalmente já é identificável, sem precisar do conteúdo completo.
        carreiras_permitidas: Nomes das carreiras já cadastradas no catálogo
            (``infrastructure.postgres.listar_carreiras``) — o modelo é restrito a uma destas ou
            a ``None``, nunca a inventar uma carreira nova.
        bancas_permitidas: Mesma restrição de ``carreiras_permitidas``, para bancas
            (``infrastructure.postgres.listar_bancas``).

    Returns:
        :class:`ClassificacaoDocumento` com o resultado da classificação.

    Raises:
        RuntimeError: Se ``carreiras_permitidas`` ou ``bancas_permitidas`` estiver vazia, se
            ``GEMINI_API_KEY`` não estiver definida, se a resposta não vier no formato esperado,
            ou se o rate limit (429) persistir depois de todas as tentativas.
        requests.HTTPError: Se a chamada HTTP falhar com qualquer erro que não seja 429.
    """
    if not carreiras_permitidas:
        raise RuntimeError(
            "Lista de carreiras permitidas está vazia — nada pra restringir a classificação do "
            "modelo. Cadastre ao menos uma carreira antes de classificar um documento."
        )
    if not bancas_permitidas:
        raise RuntimeError(
            "Lista de bancas permitidas está vazia — nada pra restringir a classificação do "
            "modelo. Cadastre ao menos uma banca antes de classificar um documento."
        )

    payload = _chamar_gemini(
        instrucao_de_sistema=_INSTRUCAO_DE_SISTEMA_CLASSIFICACAO,
        prompt=_construir_prompt_classificacao(texto_capa, carreiras_permitidas, bancas_permitidas),
        schema=_construir_schema_classificacao(carreiras_permitidas, bancas_permitidas),
    )
    return _classificacao_do_payload(payload)


def _chamar_gemini(
    *, instrucao_de_sistema: str, prompt: str, schema: dict[str, object]
) -> dict[str, Any]:
    """Chama ``generateContent`` do Gemini com retry em 429 e devolve o JSON já decodificado.

    Compartilhado entre :func:`extrair_questoes_estruturadas` e :func:`classificar_documento` —
    mesma disciplina de retry/rate-limit (ver módulo) e mesmo formato de resposta
    (``responseMimeType: application/json``); só a instrução de sistema, o prompt e o
    ``responseSchema`` variam por chamada.
    """
    api_key = obter_api_key()
    corpo_requisicao = {
        "system_instruction": {"parts": [{"text": instrucao_de_sistema}]},
        "contents": [{"parts": [{"text": prompt}]}],
        "generationConfig": {
            "responseMimeType": "application/json",
            "responseSchema": schema,
        },
    }
    url = _URL_GENERATE_CONTENT.format(modelo=_MODELO)

    for tentativa in range(1, _MAX_TENTATIVAS + 1):
        resposta = requests.post(url, params={"key": api_key}, json=corpo_requisicao, timeout=120)

        if resposta.status_code == 429:
            if tentativa == _MAX_TENTATIVAS:
                raise RuntimeError(
                    f"Rate limit do Gemini (429) persistiu depois de {_MAX_TENTATIVAS} tentativas. "
                    "Free tier do gemini-2.5-flash é limitado a 10 RPM — ver "
                    "https://ai.google.dev/gemini-api/docs/rate-limits."
                )
            espera = _extrair_retry_delay_segundos(resposta) or _ESPERA_FALLBACK_SEGUNDOS
            logger.warning(
                "Rate limit do Gemini (429) na tentativa %d/%d — aguardando %ds antes de tentar de novo.",
                tentativa,
                _MAX_TENTATIVAS,
                espera,
            )
            time.sleep(espera)
            continue

        resposta.raise_for_status()
        return _decodificar_payload(resposta.json())

    raise AssertionError("inatingível: o loop sempre retorna ou lança antes de terminar")


def _decodificar_payload(corpo_resposta: dict[str, object]) -> dict[str, Any]:
    candidatos = corpo_resposta.get("candidates")
    if not candidatos or not isinstance(candidatos, list):
        raise RuntimeError("Resposta do Gemini sem candidatos.")

    conteudo = candidatos[0].get("content", {})
    partes = conteudo.get("parts", [])
    if not partes or "text" not in partes[0]:
        raise RuntimeError("Resposta do Gemini sem texto.")

    try:
        payload: dict[str, Any] = json.loads(partes[0]["text"])
    except json.JSONDecodeError as erro:
        raise RuntimeError(f"Resposta do Gemini não é um JSON válido: {erro}") from erro
    return payload


def _construir_prompt(texto_prova: str, texto_gabarito: str, disciplinas_permitidas: list[str]) -> str:
    return f"""Texto da prova (fonte primária, extraído do PDF aplicado):
---
{texto_prova}
---

Texto do gabarito oficial (fonte primária, extraído do PDF do gabarito):
---
{texto_gabarito}
---

Disciplinas cadastradas (escolha EXATAMENTE uma destas para cada questão, sem criar nome novo):
{", ".join(disciplinas_permitidas)}

Extraia cada questão da prova como um item da lista "questoes", preenchendo:
- numero: o número da questão como aparece na prova.
- disciplina: uma das disciplinas cadastradas acima que corresponde ao conteúdo da questão.
- tipo: "multipla_escolha" se a questão tiver alternativas (A, B, C...), ou "certo_errado" se for
  do tipo Certo/Errado.
- enunciado: o enunciado completo da questão, EXATAMENTE como aparece na prova.
- alternativas: para "multipla_escolha", a lista de alternativas (letra + texto) EXATAMENTE como
  aparecem na prova; omita para "certo_errado".
- gabarito: a resposta correta, de acordo com o texto do gabarito oficial acima (a letra da
  alternativa correta, ou "Certo"/"Errado").
"""


def _construir_schema(disciplinas_permitidas: list[str]) -> dict[str, object]:
    return {
        "type": "object",
        "properties": {
            "questoes": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "numero": {"type": "integer"},
                        "disciplina": {"type": "string", "enum": disciplinas_permitidas},
                        "tipo": {"type": "string", "enum": ["certo_errado", "multipla_escolha"]},
                        "enunciado": {"type": "string"},
                        "alternativas": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "letra": {"type": "string"},
                                    "texto": {"type": "string"},
                                },
                                "required": ["letra", "texto"],
                            },
                        },
                        "gabarito": {"type": "string"},
                    },
                    "required": ["numero", "disciplina", "tipo", "enunciado", "gabarito"],
                },
            }
        },
        "required": ["questoes"],
    }


def _extrair_retry_delay_segundos(resposta: requests.Response) -> int | None:
    """Lê ``error.details[].retryDelay`` (ex.: ``"34s"``) do corpo de um 429 do Gemini.

    Melhor esforço: se o corpo não vier no formato esperado, devolve ``None``
    e quem chama cai no fallback fixo — nunca lança por causa disso.
    """
    try:
        corpo = resposta.json()
    except ValueError:
        return None

    detalhes = corpo.get("error", {}).get("details", [])
    for detalhe in detalhes:
        if detalhe.get("@type") == "type.googleapis.com/google.rpc.RetryInfo":
            retry_delay = detalhe.get("retryDelay", "")
            if isinstance(retry_delay, str) and retry_delay.endswith("s"):
                try:
                    return int(float(retry_delay[:-1]))
                except ValueError:
                    return None
    return None


def _parsear_questoes(payload: dict[str, Any]) -> list[QuestaoExtraida]:
    questoes_brutas = payload.get("questoes", [])
    questoes: list[QuestaoExtraida] = []
    for bruta in questoes_brutas:
        alternativas_brutas = bruta.get("alternativas")
        alternativas = (
            {alt["letra"]: alt["texto"] for alt in alternativas_brutas} if alternativas_brutas else None
        )
        questoes.append(
            QuestaoExtraida(
                numero=bruta["numero"],
                disciplina=bruta["disciplina"],
                tipo=bruta["tipo"],
                enunciado=bruta["enunciado"],
                alternativas=alternativas,
                gabarito=bruta["gabarito"],
            )
        )
    return questoes


def _construir_prompt_classificacao(
    texto_capa: str, carreiras_permitidas: list[str], bancas_permitidas: list[str]
) -> str:
    return f"""Texto das primeiras páginas do documento:
---
{texto_capa}
---

Carreiras cadastradas (escolha EXATAMENTE uma destas, ou null se o texto não identificar a
carreira com confiança — nunca invente um nome fora desta lista):
{", ".join(carreiras_permitidas)}

Bancas cadastradas (mesma regra de "carreira" acima, aplicada à banca organizadora):
{", ".join(bancas_permitidas)}

Classifique o documento preenchendo:
- tipo: "lei" se for um texto de lei/norma seca, "edital" se for um edital de concurso, ou
  "prova" se for uma prova aplicada (com questões e/ou gabarito).
- carreira: uma das carreiras cadastradas acima a que o documento se refere, ou null se não
  identificar isso no texto.
- banca: uma das bancas cadastradas acima que organizou/aplicou o documento, ou null se não
  identificar isso no texto.
- ano: o ano a que o documento se refere (ano do edital/prova, ou ano de publicação da lei), ou
  null se não identificar isso no texto.
"""


def _construir_schema_classificacao(
    carreiras_permitidas: list[str], bancas_permitidas: list[str]
) -> dict[str, object]:
    return {
        "type": "object",
        "properties": {
            "tipo": {"type": "string", "enum": ["lei", "edital", "prova"]},
            "carreira": {"type": "string", "enum": carreiras_permitidas, "nullable": True},
            "banca": {"type": "string", "enum": bancas_permitidas, "nullable": True},
            "ano": {"type": "integer", "nullable": True},
        },
        "required": ["tipo", "carreira", "banca", "ano"],
    }


def _classificacao_do_payload(payload: dict[str, Any]) -> ClassificacaoDocumento:
    try:
        return ClassificacaoDocumento(
            tipo=payload["tipo"],
            carreira=payload.get("carreira"),
            banca=payload.get("banca"),
            ano=payload.get("ano"),
        )
    except KeyError as erro:
        raise RuntimeError(f"Resposta do Gemini sem o campo obrigatório {erro}.") from erro
