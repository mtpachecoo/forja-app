"""Leitura de PDF compartilhada pelos dois pipelines.

Existência do arquivo → bytes → ``core.parsing.extrair_texto_pdf`` → erro
tratado. Extraído de ``ingestao_rag.ingerir_pdf`` e
``digitalizar_prova._ler_texto_pdf``, que faziam exatamente essa sequência
duas vezes com as mesmas três mensagens de erro — só a exceção lançada
(``ErroDeIngestao`` vs ``ErroDeDigitalizacao``) e o que cada pipeline
considera "vazio" (chunks vazios vs texto sem conteúdo) mudam de um pipeline
pro outro, por isso os dois ficam parametrizados aqui, não hardcoded.
"""

from __future__ import annotations

from collections.abc import Callable
from pathlib import Path

from motor_conteudo.core.parsing import extrair_texto_pdf


def ler_pdf_ou_falhar(
    caminho: Path,
    *,
    tipo_erro: type[Exception],
    checagem_vazio: Callable[[str], bool],
) -> str:
    """Lê e extrai o texto de um PDF, ou lança ``tipo_erro`` com mensagem clara.

    Args:
        caminho: Caminho do PDF no disco.
        tipo_erro: Exceção a lançar em caso de falha — cada pipeline passa a
            sua própria (``ErroDeIngestao`` ou ``ErroDeDigitalizacao``), pra
            que quem chama continue capturando só a exceção do seu domínio.
        checagem_vazio: Recebe o texto já extraído e devolve ``True`` se ele
            deve ser tratado como "nenhum conteúdo extraível". Cada pipeline
            decide o que conta como vazio pra si — ``ingerir_pdf`` verifica
            os chunks resultantes do texto, não o texto bruto;
            ``digitalizar_prova`` verifica o texto bruto diretamente.

    Returns:
        O texto extraído, quando ``checagem_vazio`` devolve ``False`` pra ele.

    Raises:
        tipo_erro: Arquivo não existe, PDF corrompido/ilegível (qualquer
            exceção do ``extrair_texto_pdf`` é envolvida aqui), ou
            ``checagem_vazio`` devolveu ``True`` pro texto extraído.
    """
    if not caminho.exists():
        raise tipo_erro(f"Arquivo não encontrado: {caminho}")

    try:
        texto = extrair_texto_pdf(caminho.read_bytes())
    except Exception as erro:
        raise tipo_erro(f"Não foi possível ler o PDF ({caminho}): {erro}") from erro

    if checagem_vazio(texto):
        raise tipo_erro(
            f"Nenhum conteúdo extraível de {caminho} — PDF vazio ou só com imagens "
            "escaneadas (sem OCR). Nada foi gravado."
        )

    return texto
