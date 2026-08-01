"""Extração de texto de PDF.

Função pura: recebe os bytes do PDF já lidos por quem chama (disco, upload,
rede — não importa) e devolve o texto extraído. Nenhum acesso a sistema de
arquivos, rede ou variável de ambiente acontece aqui — essa é uma decisão
deliberada desta fase (ver escopo do Motor de Conteúdo): I/O é
responsabilidade de uma camada externa a este módulo, ainda não construída.
"""

from __future__ import annotations

from io import BytesIO

import pdfplumber


def extrair_texto_pdf(conteudo_pdf: bytes) -> str:
    """Extrai o texto de um PDF, na ordem de leitura das páginas.

    Usa pdfplumber (extração consciente de layout) em vez de um extrator de
    fluxo simples, porque editais/leis costumam ter cabeçalho/rodapé e
    numeração de artigo que dependem de preservar a ordem de leitura real da
    página — um extrator ingênuo pode embaralhar colunas/blocos de texto,
    o que quebraria a detecção sequencial de artigos/tópicos no chunking.

    Args:
        conteudo_pdf: Bytes brutos do arquivo PDF.

    Returns:
        Texto extraído, com as páginas concatenadas por uma linha em
        branco entre elas. String vazia se nenhuma página tiver texto
        extraível (ex.: PDF só com imagens escaneadas, sem OCR).

    Raises:
        Qualquer exceção que o pdfplumber/pypdfium2 lance para bytes que não
        são um PDF válido — não é mascarada aqui; decidir o que fazer com um
        PDF corrompido é responsabilidade de quem chama, não deste núcleo puro.
    """
    textos_por_pagina: list[str] = []
    with pdfplumber.open(BytesIO(conteudo_pdf)) as pdf:
        for pagina in pdf.pages:
            texto_pagina = pagina.extract_text()
            if texto_pagina:
                textos_por_pagina.append(texto_pagina)

    return "\n\n".join(textos_por_pagina)
