"""Gera os PDFs de fixture usados pelos testes de parsing/chunking.

Roda uma vez pra produzir os .pdf commitados em tests/fixtures/ — os testes
em si não dependem do fpdf2 nem rodam este script (só leem os .pdf já
gerados). Reexecute manualmente (``uv run python tests/fixtures/gerar_fixtures.py``)
se precisar regenerar ou adicionar uma fixture nova.

Proveniência do conteúdo:
- lei_8112_arts_1_a_5.pdf: texto verídico dos Art. 1º a 5º da Lei nº
  8.112/1990 (Regime Jurídico dos Servidores Públicos Civis da União) —
  uma das leis mais citadas em editais de concurso público brasileiro.
- edital_exemplo.pdf: estrutura e linguagem representativas de um edital de
  concurso público real (numeração de tópico, formulações padrão) — não é
  cópia literal de um edital específico, é a formulação genérica que se
  repete quase idêntica entre milhares de editais reais.
"""

from __future__ import annotations

from pathlib import Path

from fpdf import FPDF

DIRETORIO_FIXTURES = Path(__file__).parent

TEXTO_LEI_8112 = """\
LEI Nº 8.112, DE 11 DE DEZEMBRO DE 1990

Art. 1º Esta Lei institui o Regime Jurídico dos Servidores Públicos Civis \
da União, das autarquias, inclusive as em regime especial, e das fundações \
públicas federais.

Art. 2º Para os efeitos desta Lei, servidor é a pessoa legalmente \
investida em cargo público.

Art. 3º Cargo público é o conjunto de atribuições e responsabilidades \
previstas na estrutura organizacional que devem ser cometidas a um servidor.
Parágrafo único.  Os cargos públicos, acessíveis a todos os brasileiros, \
são criados por lei, com denominação própria e vencimento pago pelos \
cofres públicos, para provimento em caráter efetivo ou em comissão.

Art. 4º É proibida a prestação de serviços gratuitos, ressalvados os \
casos previstos em lei.

Art. 5º São requisitos básicos para investidura em cargo público:
I - a nacionalidade brasileira;
II - o gozo dos direitos políticos;
III - a quitação com as obrigações militares e eleitorais;
IV - o nível de escolaridade exigido para o exercício do cargo;
V - a idade mínima de dezoito anos;
VI - aptidão física e mental.
"""

TEXTO_EDITAL_EXEMPLO = """\
EDITAL Nº 1/2026 - CONCURSO PÚBLICO

1. DAS DISPOSIÇÕES PRELIMINARES

1.1 O presente Edital contém as normas referentes à realização do \
concurso público destinado ao provimento de vagas do cargo indicado no \
Anexo I.
1.2 O concurso público será regido por este Edital e seus anexos, \
executado pela banca organizadora.

2. DOS REQUISITOS PARA INVESTIDURA NO CARGO

2.1 São requisitos básicos para a investidura no cargo:
2.1.1 ter sido aprovado e classificado no concurso público, na forma \
estabelecida neste Edital;
2.1.2 ter nacionalidade brasileira ou portuguesa;
2.1.3 estar em dia com as obrigações eleitorais.

3. DAS INSCRIÇÕES

3.1 A inscrição do candidato implicará o conhecimento e a tácita \
aceitação das normas e condições estabelecidas neste Edital.
"""


def _gerar_pdf(texto: str, caminho_saida: Path) -> None:
    pdf = FPDF()
    pdf.add_page()
    # Fonte TTF embutida (não um core font tipo "helvetica"): sem uma fonte
    # embutida com CMap Unicode, o pdfplumber/pdfminer não consegue
    # re-extrair acentos corretamente a partir do PDF gerado (vira "�").
    pdf.add_font("Corpo", "", str(_caminho_fonte_com_acentuacao()))
    pdf.set_font("Corpo", size=11)
    pdf.multi_cell(0, 6, texto)
    pdf.output(str(caminho_saida))


def _caminho_fonte_com_acentuacao() -> Path:
    """Localiza uma TTF Unicode instalada no sistema, só pra gerar as fixtures."""
    candidatas = [
        Path("C:/Windows/Fonts/arial.ttf"),
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
        Path("/System/Library/Fonts/Supplemental/Arial.ttf"),
    ]
    for candidata in candidatas:
        if candidata.exists():
            return candidata

    raise FileNotFoundError(
        "Nenhuma fonte TTF Unicode encontrada nos caminhos conhecidos "
        f"({[str(c) for c in candidatas]}) — ajuste a lista pro seu sistema."
    )


def main() -> None:
    _gerar_pdf(TEXTO_LEI_8112, DIRETORIO_FIXTURES / "lei_8112_arts_1_a_5.pdf")
    _gerar_pdf(TEXTO_EDITAL_EXEMPLO, DIRETORIO_FIXTURES / "edital_exemplo.pdf")
    print("Fixtures geradas em", DIRETORIO_FIXTURES)


if __name__ == "__main__":
    main()
