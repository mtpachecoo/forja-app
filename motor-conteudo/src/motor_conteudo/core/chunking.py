"""Divisão de texto em chunks por artigo/tópico.

Função pura: recebe uma string já extraída (ver ``core.parsing``) e devolve
uma lista de :class:`Chunk`. Nenhum I/O acontece aqui.

Decisão de design (reaproveitada do protótipo anterior): chunking por
artigo/tópico, não por tamanho fixo de caractere. Um corte por tamanho fixo
quebraria um artigo no meio, o que é ruim tanto pra apresentar o conteúdo
quanto pra qualidade de embeddings/RAG — cada chunk aqui é uma unidade de
sentido completa (um artigo, um capítulo, um tópico numerado de edital).

Limitação conhecida do heurístico atual: a detecção de cabeçalho é
baseada em regex sobre o início de cada linha (após ``strip()``). Cobre os
dois formatos mais comuns em editais/leis brasileiros:

- Artigo de lei: ``Art. 5º``, ``Artigo 5``, ``Art. 5º-A`` (com ou sem corpo
  na mesma linha do marcador).
- Divisão estrutural: ``CAPÍTULO I``, ``TÍTULO II``, ``Seção III``.
- Tópico numerado de edital: ``1. DAS DISPOSIÇÕES PRELIMINARES``,
  ``2. DOS REQUISITOS...`` — número seguido de uma palavra em CAIXA ALTA
  (2+ letras), padrão típico de título de seção em edital. Deliberadamente
  só o nível de topo: um item como ``1.1 O presente Edital...`` começa com
  maiúscula só por ser início de frase (convenção do português, não marca
  de título) e fica como corpo do tópico "1.", não vira chunk novo — do
  contrário cada subitem numerado (``1.1``, ``2.1.3``...) fragmentaria o
  chunk em pedaços pequenos demais pra fazer sentido sozinhos.

PDFs com formatação muito irregular (ex.: marcador de artigo quebrado em
duas linhas pela extração) podem não ser detectados; nesse caso o texto cai
no chunk anterior em vez de abrir um novo, silenciosamente — não há
lançamento de erro para heurística de detecção que não bate. Da mesma
forma, uma frase numerada que comece com uma sigla em caixa alta (ex.:
``1. CPF do candidato...``) pode ser reconhecida como título por engano —
é o trade-off de um heurístico baseado em regex sem análise semântica.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

_PADRAO_ARTIGO = re.compile(
    r"^Art(?:igo)?\.?\s*\d+[º°oª]?(?:-[A-Z])?\.?",
    re.IGNORECASE,
)
_PADRAO_DIVISAO_ESTRUTURAL = re.compile(
    r"^(CAP[ÍI]TULO|T[ÍI]TULO|SE[ÇC][ÃA]O)\s+[IVXLCDM]+\b",
    re.IGNORECASE,
)
_PADRAO_TOPICO_NUMERADO = re.compile(
    r"^\d+(?:\.\d+)*[.\-)]?\s+[A-ZÀ-Ÿ]{2,}(?:\s|$)"
)


@dataclass(frozen=True)
class Chunk:
    """Um trecho de texto delimitado por um artigo/tópico.

    Attributes:
        titulo: O marcador detectado (ex.: ``"Art. 5º"``, ``"CAPÍTULO I"``,
            ``"1. DAS DISPOSIÇÕES PRELIMINARES"``), ou string vazia se o
            chunk é o preâmbulo — texto antes do primeiro marcador
            reconhecido.
        texto: Conteúdo completo do chunk, incluindo a própria linha do
            marcador (quando ela também é corpo, como em ``"Art. 5º Compete
            à União..."``).
    """

    titulo: str
    texto: str


def dividir_em_chunks(texto: str) -> list[Chunk]:
    """Divide o texto em chunks por artigo/tópico (ver módulo para a heurística).

    Args:
        texto: Texto já extraído (ex.: saída de
            ``core.parsing.extrair_texto_pdf``).

    Returns:
        Lista de :class:`Chunk`, na ordem em que os marcadores aparecem no
        texto. Lista vazia se ``texto`` for vazio ou só espaço em branco.
        Chunks vazios (sem nenhum conteúdo após ``strip()``) não são
        incluídos — um marcador seguido imediatamente por outro marcador,
        sem nada entre os dois, não gera chunk fantasma.
    """
    chunks: list[Chunk] = []
    titulo_atual = ""
    linhas_atual: list[str] = []

    def fechar_chunk_atual() -> None:
        conteudo = "\n".join(linhas_atual).strip()
        # conteudo == titulo_atual significa que a única linha acumulada foi
        # a própria linha do marcador (CAPÍTULO/tópico, onde título = linha
        # inteira) sem nenhum corpo depois — não é conteúdo útil sozinho.
        # Não afeta Art. N com corpo na mesma linha (título ali é só o
        # trecho do marcador, não a linha toda) nem o preâmbulo (titulo_atual
        # começa como "", nunca igual a um conteúdo não vazio).
        if conteudo and conteudo != titulo_atual:
            chunks.append(Chunk(titulo=titulo_atual, texto=conteudo))

    for linha_bruta in texto.splitlines():
        linha = linha_bruta.strip()
        titulo_detectado = _detectar_titulo(linha)

        if titulo_detectado is not None:
            fechar_chunk_atual()
            titulo_atual = titulo_detectado
            linhas_atual = [linha_bruta]
        else:
            linhas_atual.append(linha_bruta)

    fechar_chunk_atual()
    return chunks


def _detectar_titulo(linha: str) -> str | None:
    """Reconhece se ``linha`` abre um novo chunk; devolve o título, ou ``None``."""
    if not linha:
        return None

    correspondencia_artigo = _PADRAO_ARTIGO.match(linha)
    if correspondencia_artigo is not None:
        return correspondencia_artigo.group().strip().rstrip(".")

    if _PADRAO_DIVISAO_ESTRUTURAL.match(linha) is not None:
        return linha

    if _PADRAO_TOPICO_NUMERADO.match(linha) is not None:
        return linha

    return None
