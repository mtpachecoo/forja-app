"""Pipeline de lote: varre uma pasta de PDFs, classifica e grava cada um sem confirmação.

Diferente de ``cli._comando_processar`` (interativo, um arquivo por vez, sempre com
confirmação manual e input pra campo incerto), aqui o lote roda direto: cada item é
classificado e gravado sem perguntar nada, e uma falha num item não interrompe os
demais — o efeito observável é só o log por item (auditoria) e o relatório final
(quantos deram certo, quantos falharam).

Pareamento prova+gabarito por nome de arquivo (não por conteúdo, ver
:func:`_parear_gabaritos`): qualquer PDF cujo nome contenha "gabarito" nunca é
processado como documento próprio — só serve de gabarito pro PDF de prova cujo
nome bate (mesma "chave" depois de remover os termos "prova"/"gabarito" do nome).
Um gabarito sem par correspondente vira um item falhado (nada a fazer com um
gabarito órfão sozinho).

Sem confirmação manual disponível, qualquer campo que a classificação não
identificar com confiança falha o item em vez de adivinhar (mesma disciplina do
comando ``processar``, só que aqui não há prompt pra completar o campo à mão):
carreira e banca precisam bater com o catálogo cadastrado, ano precisa vir
preenchido. O ``edital_id`` (obrigatório em ``digitalizar_prova``, nunca inferido
pela classificação) é resolvido buscando no catálogo o edital já cadastrado pra
essa combinação carreira/banca/ano (``infrastructure.postgres.buscar_edital_id``);
se não achar exatamente um, o item falha em vez de gravar num edital errado.
"""

from __future__ import annotations

import logging
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from uuid import UUID

import psycopg

from motor_conteudo.infrastructure.llm import ClassificacaoDocumento, classificar_documento
from motor_conteudo.infrastructure.postgres import (
    TipoFonte,
    buscar_edital_id,
    conectar,
    listar_bancas,
    listar_carreiras,
)
from motor_conteudo.pipelines.digitalizar_prova import digitalizar_prova
from motor_conteudo.pipelines.ingestao_rag import ingerir_pdf
from motor_conteudo.pipelines.leitura_pdf import ler_pdf_ou_falhar

logger = logging.getLogger(__name__)

_TAMANHO_TEXTO_CAPA = 6000  # caracteres — mesmo recorte do comando `processar` interativo
_PADRAO_TERMOS_PAREAMENTO = re.compile(r"[_\-\s]*(prova|gabarito)[_\-\s]*", re.IGNORECASE)


class ErroDeProcessamentoEmLote(Exception):
    """Erro que impede o lote inteiro de rodar (pasta inexistente ou sem PDF nenhum).

    Diferente de uma falha item a item (capturada e reportada em
    :class:`ItemProcessado`, sem interromper os demais), isto só é lançado quando
    não há nada a processar. Mensagem já pensada pra ser mostrada direto na CLI.
    """


@dataclass(frozen=True)
class ItemProcessado:
    """Resultado do processamento de um único arquivo do lote.

    Attributes:
        arquivo: Caminho do PDF processado (ou do gabarito órfão, quando
            ``sucesso`` é ``False`` por falta de par).
        sucesso: ``True`` se classificado e gravado sem erro.
        classificacao: Classificação obtida do Gemini, ou ``None`` se a falha
            aconteceu antes da classificação (ex.: PDF ilegível) ou se o item é
            um gabarito órfão (nunca chega a ser classificado).
        mensagem: Descrição do resultado — pensada pra log/auditoria, não um
            traceback cru.
    """

    arquivo: Path
    sucesso: bool
    classificacao: ClassificacaoDocumento | None
    mensagem: str


@dataclass(frozen=True)
class RelatorioProcessamento:
    """Relatório final de uma chamada a :func:`processar_pasta`."""

    itens: list[ItemProcessado]

    @property
    def quantidade_sucesso(self) -> int:
        return sum(1 for item in self.itens if item.sucesso)

    @property
    def quantidade_falha(self) -> int:
        return sum(1 for item in self.itens if not item.sucesso)


def processar_pasta(caminho_pasta: Path) -> RelatorioProcessamento:
    """Varre ``caminho_pasta``, pareia prova+gabarito por nome, classifica e grava cada item.

    Não recursivo: só os arquivos ``*.pdf`` diretamente dentro de ``caminho_pasta``
    entram no lote. A ordem de processamento é a ordem alfabética do nome do
    arquivo — determinística, pra o relatório/log ficarem reproduzíveis entre
    execuções.

    Args:
        caminho_pasta: Pasta com os PDFs a processar.

    Returns:
        :class:`RelatorioProcessamento` com um :class:`ItemProcessado` por
        arquivo (inclusive gabaritos órfãos) — nunca lança por causa de um item
        individual ruim, só por uma condição que impede o lote inteiro de rodar.

    Raises:
        ErroDeProcessamentoEmLote: ``caminho_pasta`` não existe/não é uma pasta,
            ou não tem nenhum PDF dentro.
    """
    if not caminho_pasta.is_dir():
        raise ErroDeProcessamentoEmLote(f"Pasta não encontrada: {caminho_pasta}")

    arquivos_pdf = sorted(
        caminho for caminho in caminho_pasta.iterdir() if caminho.is_file() and caminho.suffix.lower() == ".pdf"
    )
    if not arquivos_pdf:
        raise ErroDeProcessamentoEmLote(f"Nenhum PDF encontrado em {caminho_pasta}.")

    gabaritos = [caminho for caminho in arquivos_pdf if _eh_gabarito(caminho)]
    documentos = [caminho for caminho in arquivos_pdf if not _eh_gabarito(caminho)]
    pares_gabarito = _parear_gabaritos(documentos, gabaritos)
    gabaritos_orfaos = [gabarito for gabarito in gabaritos if gabarito not in pares_gabarito.values()]

    itens: list[ItemProcessado] = []

    for gabarito in gabaritos_orfaos:
        item = ItemProcessado(
            arquivo=gabarito,
            sucesso=False,
            classificacao=None,
            mensagem="Gabarito sem prova correspondente pelo nome do arquivo — nada foi gravado.",
        )
        itens.append(item)
        logger.warning("FALHA %s: %s", item.arquivo.name, item.mensagem)

    with conectar() as conn:
        carreiras = listar_carreiras(conn)
        bancas = listar_bancas(conn)

        for documento in documentos:
            item = _processar_um(documento, pares_gabarito.get(documento), conn=conn, carreiras=carreiras, bancas=bancas)
            itens.append(item)
            if item.sucesso:
                tipo = item.classificacao.tipo if item.classificacao is not None else "?"
                logger.info("OK %s (%s): %s", item.arquivo.name, tipo, item.mensagem)
            else:
                logger.warning("FALHA %s: %s", item.arquivo.name, item.mensagem)

    return RelatorioProcessamento(itens=itens)


def _eh_gabarito(caminho: Path) -> bool:
    return "gabarito" in caminho.stem.lower()


def _chave_pareamento(stem: str) -> str:
    chave = _PADRAO_TERMOS_PAREAMENTO.sub("_", stem)
    chave = re.sub(r"[_\-\s]+", "_", chave)
    return chave.strip("_").lower()


def _parear_gabaritos(documentos: list[Path], gabaritos: list[Path]) -> dict[Path, Path]:
    """Mapeia cada documento pro seu gabarito, quando a chave do nome bate.

    O match é só estrutural (nome de arquivo), nunca de conteúdo — ver módulo.
    Documentos sem gabarito correspondente ficam de fora do mapa: podem
    legitimamente ser lei/edital, ou uma prova com gabarito já embutido no
    próprio PDF.
    """
    chaves_documentos = {_chave_pareamento(documento.stem): documento for documento in documentos}
    pares: dict[Path, Path] = {}
    for gabarito in gabaritos:
        documento = chaves_documentos.get(_chave_pareamento(gabarito.stem))
        if documento is not None:
            pares[documento] = gabarito
    return pares


def _processar_um(
    documento: Path,
    gabarito: Path | None,
    *,
    conn: psycopg.Connection[Any],
    carreiras: dict[str, UUID],
    bancas: dict[str, UUID],
) -> ItemProcessado:
    classificacao: ClassificacaoDocumento | None = None
    try:
        texto = ler_pdf_ou_falhar(documento, tipo_erro=RuntimeError, checagem_vazio=lambda texto: not texto.strip())
        classificacao = classificar_documento(
            texto[:_TAMANHO_TEXTO_CAPA],
            carreiras_permitidas=list(carreiras),
            bancas_permitidas=list(bancas),
        )
        mensagem = _gravar(documento, gabarito, classificacao, conn=conn, carreiras=carreiras, bancas=bancas)
        return ItemProcessado(arquivo=documento, sucesso=True, classificacao=classificacao, mensagem=mensagem)
    except Exception as erro:
        return ItemProcessado(arquivo=documento, sucesso=False, classificacao=classificacao, mensagem=str(erro))


def _gravar(
    documento: Path,
    gabarito: Path | None,
    classificacao: ClassificacaoDocumento,
    *,
    conn: psycopg.Connection[Any],
    carreiras: dict[str, UUID],
    bancas: dict[str, UUID],
) -> str:
    if classificacao.tipo != "prova":
        resultado_ingestao = ingerir_pdf(documento, tipo=TipoFonte(classificacao.tipo))
        if resultado_ingestao.pulou:
            return "Pulado — já estava atualizado (RN-014)."
        return (
            f"Ingerido: {resultado_ingestao.quantidade_chunks} chunk(s) gravado(s) "
            f"(fonte {resultado_ingestao.fonte_id})."
        )

    if classificacao.carreira is None or classificacao.carreira not in carreiras:
        raise RuntimeError(
            f"Carreira não identificada ou fora do catálogo ({classificacao.carreira!r}) — "
            "sem confirmação manual em lote, item não gravado."
        )
    if classificacao.banca is None or classificacao.banca not in bancas:
        raise RuntimeError(
            f"Banca não identificada ou fora do catálogo ({classificacao.banca!r}) — obrigatória em "
            "lote pra localizar o edital cadastrado, item não gravado."
        )
    if classificacao.ano is None:
        raise RuntimeError("Ano não identificado — sem confirmação manual em lote, item não gravado.")

    carreira_id = carreiras[classificacao.carreira]
    banca_id = bancas[classificacao.banca]
    edital_id = buscar_edital_id(conn, carreira_id=carreira_id, banca_id=banca_id, ano=classificacao.ano)
    if edital_id is None:
        raise RuntimeError(
            f"Nenhum edital único cadastrado pra carreira={classificacao.carreira!r}, "
            f"banca={classificacao.banca!r}, ano={classificacao.ano} — cadastre o edital antes de "
            "processar em lote."
        )

    resultado_prova = digitalizar_prova(
        documento, gabarito, carreira_id=carreira_id, banca_id=banca_id, edital_id=edital_id, ano=classificacao.ano
    )
    if resultado_prova.pulou:
        return "Pulado — já existiam questões gravadas para esse edital (idempotência)."
    return (
        f"Digitalizado: {resultado_prova.quantidade_questoes} questão(ões) gravada(s) em revisão "
        f"(fonte {resultado_prova.fonte_prova_id})."
    )
