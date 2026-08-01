"""Testes de motor_conteudo.core.chunking."""

from __future__ import annotations

from motor_conteudo.core.chunking import Chunk, dividir_em_chunks
from motor_conteudo.core.parsing import extrair_texto_pdf


def test_chunking_lei_divide_por_artigo(bytes_lei_8112: bytes) -> None:
    texto = extrair_texto_pdf(bytes_lei_8112)

    chunks = dividir_em_chunks(texto)

    titulos = [chunk.titulo for chunk in chunks]
    # Primeiro chunk é o preâmbulo (título da lei, antes do Art. 1º) —
    # título vazio, mas não descartado: tem conteúdo próprio.
    assert titulos == ["", "Art. 1º", "Art. 2º", "Art. 3º", "Art. 4º", "Art. 5º"]
    assert "LEI Nº 8.112" in chunks[0].texto


def test_chunking_lei_agrupa_paragrafo_unico_no_artigo_correspondente(bytes_lei_8112: bytes) -> None:
    texto = extrair_texto_pdf(bytes_lei_8112)

    chunks = dividir_em_chunks(texto)

    art_3 = next(c for c in chunks if c.titulo == "Art. 3º")
    assert "Parágrafo único" in art_3.texto
    assert "Cargo público" in art_3.texto


def test_chunking_artigo_sem_subtopico_vira_um_unico_chunk_completo(bytes_lei_8112: bytes) -> None:
    # Art. 4º da fixture é uma frase única, sem parágrafo/inciso — caso de
    # borda "artigo sem subtópico": não deve virar múltiplos chunks nem
    # perder conteúdo.
    texto = extrair_texto_pdf(bytes_lei_8112)

    chunks = dividir_em_chunks(texto)

    art_4 = next(c for c in chunks if c.titulo == "Art. 4º")
    assert "proibida a prestação de serviços gratuitos" in art_4.texto
    # E nada do Art. 5º vazou pro chunk do Art. 4º.
    assert "Art. 5º" not in art_4.texto


def test_chunking_edital_divide_por_topico_de_topo_nao_por_subitem(bytes_edital_exemplo: bytes) -> None:
    texto = extrair_texto_pdf(bytes_edital_exemplo)

    chunks = dividir_em_chunks(texto)

    titulos = [chunk.titulo for chunk in chunks]
    # Primeiro chunk é o preâmbulo ("EDITAL Nº..."), antes do primeiro
    # tópico numerado — título vazio, mas com conteúdo próprio.
    assert titulos == [
        "",
        "1. DAS DISPOSIÇÕES PRELIMINARES",
        "2. DOS REQUISITOS PARA INVESTIDURA NO CARGO",
        "3. DAS INSCRIÇÕES",
    ]
    # Subitens (1.1, 2.1.1 etc.) ficam como corpo do tópico pai, não viram
    # chunk à parte — do contrário cada cláusula numerada fragmentaria o
    # conteúdo em pedaços pequenos demais pra fazer sentido isolados.
    topico_2 = chunks[2]
    assert "2.1.1" in topico_2.texto
    assert "2.1.2" in topico_2.texto


def test_texto_vazio_retorna_lista_vazia() -> None:
    assert dividir_em_chunks("") == []
    assert dividir_em_chunks("   \n\n   ") == []


def test_texto_sem_nenhum_marcador_reconhecido_vira_um_chunk_de_preambulo() -> None:
    chunks = dividir_em_chunks("Só um texto solto, sem nenhum Art. ou tópico numerado.")

    assert len(chunks) == 1
    assert chunks[0].titulo == ""
    assert "texto solto" in chunks[0].texto


def test_formatacao_irregular_com_linhas_em_branco_extras_e_espacos_nao_quebra_chunking() -> None:
    texto_irregular = (
        "\n\n   Art. 1º    Disposição inicial, com espaços estranhos.   \n"
        "\n\n\n"
        "   continuação do Art. 1º depois de várias linhas em branco.\n"
        "\n"
        "Art.2ºSem espaço nenhum depois do ponto, colado no número.\n"
    )

    chunks = dividir_em_chunks(texto_irregular)

    # Título reflete o texto de origem tal qual (sem normalizar espaçamento)
    # — "Art.2º" colado permanece colado no título; o que importa aqui é que
    # a fronteira do chunk foi detectada mesmo sem espaço nenhum.
    assert [c.titulo for c in chunks] == ["Art. 1º", "Art.2º"]
    assert "continuação do Art. 1º" in chunks[0].texto
    assert "colado no número" in chunks[1].texto


def test_marcador_seguido_imediatamente_de_outro_marcador_nao_gera_chunk_fantasma() -> None:
    texto = (
        "CAPÍTULO I\n"
        "CAPÍTULO II\n"
        "Disposições gerais do capítulo dois.\n"
        "Art. 1º Conteúdo real aqui.\n"
    )

    chunks = dividir_em_chunks(texto)

    # "CAPÍTULO I" não tem nenhum conteúdo próprio antes do próximo marcador
    # ("CAPÍTULO II" vem logo em seguida) — não deveria gerar um chunk vazio.
    # "CAPÍTULO II" já tem corpo próprio antes do Art. 1º, então é mantido.
    assert [c.titulo for c in chunks] == ["CAPÍTULO II", "Art. 1º"]
    assert "Disposições gerais do capítulo dois." in chunks[0].texto


def test_chunk_e_dataclass_imutavel_com_campos_titulo_e_texto() -> None:
    chunk = Chunk(titulo="Art. 1º", texto="conteúdo")

    assert chunk.titulo == "Art. 1º"
    assert chunk.texto == "conteúdo"
