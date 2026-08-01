"""Testes de motor_conteudo.pipelines.digitalizar_prova.

Dois grupos, como pedido pro isolamento de cota de API:

- Orquestração (LLM mockado, Postgres real): confirma que o parsing da saída
  estruturada e a gravação em ``questoes``/``fontes_conteudo`` funcionam,
  sem gastar cota do Gemini.
- Integração real (LLM + Postgres reais, `pytestmark` mais restrito):
  confirma o caminho de ponta a ponta com uma prova de exemplo pequena.

Cada teste que grava dado cria suas próprias linhas de apoio (carreira, banca,
disciplina) e limpa tudo depois de si — mesmo padrão de
``tests/pipelines/test_ingestao_rag.py``.
"""

from __future__ import annotations

import os
import uuid
from pathlib import Path
from unittest.mock import MagicMock
from uuid import UUID

import pytest
from fpdf import FPDF

from motor_conteudo.infrastructure.llm import QuestaoExtraida
from motor_conteudo.infrastructure.postgres import conectar
from motor_conteudo.pipelines.digitalizar_prova import ErroDeDigitalizacao, digitalizar_prova

pytestmark = pytest.mark.skipif(
    not os.environ.get("DATABASE_URL"),
    reason="DATABASE_URL não definida — teste de pipeline precisa de Postgres real.",
)


class _RegistrosDeApoio:
    def __init__(
        self,
        carreira_id: UUID,
        banca_id: UUID,
        disciplina_id: UUID,
        disciplina_nome: str,
        edital_id: UUID,
    ) -> None:
        self.carreira_id = carreira_id
        self.banca_id = banca_id
        self.disciplina_id = disciplina_id
        self.disciplina_nome = disciplina_nome
        self.edital_id = edital_id


def _criar_registros_de_apoio(sufixo: str, ano: int = 2026) -> _RegistrosDeApoio:
    with conectar() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "INSERT INTO carreiras (nome, orgao) VALUES (%s, %s) RETURNING id",
                (f"Carreira de teste {sufixo}", "Órgão de teste"),
            )
            carreira_row = cur.fetchone()
            assert carreira_row is not None

            cur.execute(
                "INSERT INTO bancas (nome) VALUES (%s) RETURNING id", (f"Banca de teste {sufixo}",)
            )
            banca_row = cur.fetchone()
            assert banca_row is not None

            disciplina_nome = f"Disciplina de teste {sufixo}"
            cur.execute(
                "INSERT INTO disciplinas (nome) VALUES (%s) RETURNING id", (disciplina_nome,)
            )
            disciplina_row = cur.fetchone()
            assert disciplina_row is not None

            # fontes_conteudo.edital_id tem FK pra editais(id) — o pipeline assume que quem
            # roda já sabe de qual edital cadastrado é a prova, não é um UUID solto.
            cur.execute(
                "INSERT INTO editais (carreira_id, banca_id, ano) VALUES (%s, %s, %s) RETURNING id",
                (carreira_row[0], banca_row[0], ano),
            )
            edital_row = cur.fetchone()
            assert edital_row is not None

        conn.commit()

    return _RegistrosDeApoio(
        carreira_id=UUID(str(carreira_row[0])),
        banca_id=UUID(str(banca_row[0])),
        disciplina_id=UUID(str(disciplina_row[0])),
        disciplina_nome=disciplina_nome,
        edital_id=UUID(str(edital_row[0])),
    )


def _limpar_tudo(registros: _RegistrosDeApoio) -> None:
    with conectar() as conn:
        with conn.cursor() as cur:
            cur.execute(
                "DELETE FROM questoes WHERE fonte_prova_id IN "
                "(SELECT id FROM fontes_conteudo WHERE edital_id = %s)",
                (registros.edital_id,),
            )
            cur.execute("DELETE FROM fontes_conteudo WHERE edital_id = %s", (registros.edital_id,))
            cur.execute("DELETE FROM editais WHERE id = %s", (registros.edital_id,))
            cur.execute("DELETE FROM disciplinas WHERE id = %s", (registros.disciplina_id,))
            cur.execute("DELETE FROM carreiras WHERE id = %s", (registros.carreira_id,))
            cur.execute("DELETE FROM bancas WHERE id = %s", (registros.banca_id,))
        conn.commit()


def _pdf_minimo(destino: Path, texto: str) -> None:
    pdf = FPDF()
    pdf.add_page()
    pdf.set_font("helvetica", size=11)
    pdf.multi_cell(0, 6, texto)
    destino.write_bytes(bytes(pdf.output()))


def _questoes_fake(disciplina_nome: str) -> list[QuestaoExtraida]:
    return [
        QuestaoExtraida(
            numero=1,
            disciplina=disciplina_nome,
            tipo="multipla_escolha",
            enunciado="Enunciado da questão 1, fixo pro teste de orquestração.",
            alternativas={"A": "Primeira alternativa.", "B": "Segunda alternativa."},
            gabarito="B",
        ),
        QuestaoExtraida(
            numero=2,
            disciplina=disciplina_nome,
            tipo="certo_errado",
            enunciado="Enunciado da questão 2, fixo pro teste de orquestração.",
            alternativas=None,
            gabarito="Certo",
        ),
    ]


def test_digitalizacao_grava_questoes_com_status_em_revisao_e_citacao_completa(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    sufixo = str(uuid.uuid4())
    registros = _criar_registros_de_apoio(sufixo)
    ano = 2026

    chamada_mockada = MagicMock(return_value=_questoes_fake(registros.disciplina_nome))
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas", chamada_mockada
    )

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_minimo(caminho_prova, "Texto da prova, irrelevante - o LLM esta mockado neste teste.")

    try:
        resultado = digitalizar_prova(
            caminho_prova,
            None,
            carreira_id=registros.carreira_id,
            banca_id=registros.banca_id,
            edital_id=registros.edital_id,
            ano=ano,
        )

        assert not resultado.pulou
        assert resultado.fonte_prova_id is not None
        assert resultado.quantidade_questoes == 2

        # O pipeline restringiu o LLM ao catálogo real de disciplinas — não a uma lista fixa.
        _texto_prova, _texto_gabarito = chamada_mockada.call_args[0]
        disciplinas_passadas = chamada_mockada.call_args.kwargs["disciplinas_permitidas"]
        assert registros.disciplina_nome in disciplinas_passadas

        with conectar() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT tipo, edital_id FROM fontes_conteudo WHERE id = %s",
                    (resultado.fonte_prova_id,),
                )
                fonte = cur.fetchone()

                cur.execute(
                    """
                    SELECT tipo, status, origem, carreira_id, banca_id, disciplina_id, ano,
                           alternativas, gabarito, explicacao
                    FROM questoes
                    WHERE fonte_prova_id = %s
                    ORDER BY gabarito
                    """,
                    (resultado.fonte_prova_id,),
                )
                linhas = cur.fetchall()

        assert fonte is not None
        assert fonte[0] == "prova"
        assert UUID(str(fonte[1])) == registros.edital_id

        assert len(linhas) == 2
        for tipo, status, origem, carreira_id, banca_id, disciplina_id, ano_gravado, _alt, _gab, explicacao in linhas:
            assert status == "em_revisao"
            assert origem == "reproduzida_prova_oficial"
            assert UUID(str(carreira_id)) == registros.carreira_id
            assert UUID(str(banca_id)) == registros.banca_id
            assert UUID(str(disciplina_id)) == registros.disciplina_id
            assert ano_gravado == ano
            assert explicacao  # NOT NULL respeitado, nunca vazio

        tipos = {linha[0] for linha in linhas}
        assert tipos == {"multipla_escolha", "certo_errado"}

        linha_multipla = next(linha for linha in linhas if linha[0] == "multipla_escolha")
        assert linha_multipla[7] == {"A": "Primeira alternativa.", "B": "Segunda alternativa."}
        linha_certo_errado = next(linha for linha in linhas if linha[0] == "certo_errado")
        assert linha_certo_errado[7] is None
    finally:
        _limpar_tudo(registros)


def test_digitalizacao_repetida_pula_e_nao_duplica(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    sufixo = str(uuid.uuid4())
    registros = _criar_registros_de_apoio(sufixo)

    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas",
        MagicMock(return_value=_questoes_fake(registros.disciplina_nome)),
    )

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_minimo(caminho_prova, "Texto da prova, irrelevante - o LLM esta mockado neste teste.")

    try:
        primeira_vez = digitalizar_prova(
            caminho_prova,
            None,
            carreira_id=registros.carreira_id,
            banca_id=registros.banca_id,
            edital_id=registros.edital_id,
            ano=2026,
        )
        assert not primeira_vez.pulou

        segunda_vez = digitalizar_prova(
            caminho_prova,
            None,
            carreira_id=registros.carreira_id,
            banca_id=registros.banca_id,
            edital_id=registros.edital_id,
            ano=2026,
        )

        assert segunda_vez.pulou is True
        assert segunda_vez.fonte_prova_id is None
        assert segunda_vez.quantidade_questoes == 0

        with conectar() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT count(*) FROM fontes_conteudo WHERE edital_id = %s", (registros.edital_id,)
                )
                total_fontes = cur.fetchone()
                assert total_fontes is not None
                assert total_fontes[0] == 1

                cur.execute(
                    "SELECT count(*) FROM questoes q JOIN fontes_conteudo f ON f.id = q.fonte_prova_id "
                    "WHERE f.edital_id = %s",
                    (registros.edital_id,),
                )
                total_questoes = cur.fetchone()
                assert total_questoes is not None
                assert total_questoes[0] == 2
    finally:
        _limpar_tudo(registros)


def test_disciplina_fora_do_catalogo_nao_grava_nada(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> None:
    sufixo = str(uuid.uuid4())
    registros = _criar_registros_de_apoio(sufixo)

    questoes_com_disciplina_invalida = [
        QuestaoExtraida(
            numero=1,
            disciplina="Disciplina que não existe no catálogo",
            tipo="certo_errado",
            enunciado="Enunciado qualquer.",
            alternativas=None,
            gabarito="Certo",
        )
    ]
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas",
        MagicMock(return_value=questoes_com_disciplina_invalida),
    )

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_minimo(caminho_prova, "Texto da prova, irrelevante - o LLM esta mockado neste teste.")

    try:
        with pytest.raises(ErroDeDigitalizacao, match="não está no catálogo"):
            digitalizar_prova(
                caminho_prova,
                None,
                carreira_id=registros.carreira_id,
                banca_id=registros.banca_id,
                edital_id=registros.edital_id,
                ano=2026,
            )

        with conectar() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT count(*) FROM fontes_conteudo WHERE edital_id = %s", (registros.edital_id,)
                )
                total = cur.fetchone()
                assert total is not None
                assert total[0] == 0
    finally:
        _limpar_tudo(registros)


def test_gabarito_sem_alternativa_correspondente_nao_grava_nada(
    tmp_path: Path, monkeypatch: pytest.MonkeyPatch
) -> None:
    sufixo = str(uuid.uuid4())
    registros = _criar_registros_de_apoio(sufixo)

    questoes_com_gabarito_inconsistente = [
        QuestaoExtraida(
            numero=1,
            disciplina=registros.disciplina_nome,
            tipo="multipla_escolha",
            enunciado="Enunciado qualquer.",
            alternativas={"A": "Única alternativa extraída."},
            gabarito="Z",  # não corresponde a nenhuma alternativa extraída
        )
    ]
    monkeypatch.setattr(
        "motor_conteudo.pipelines.digitalizar_prova.extrair_questoes_estruturadas",
        MagicMock(return_value=questoes_com_gabarito_inconsistente),
    )

    caminho_prova = tmp_path / "prova.pdf"
    _pdf_minimo(caminho_prova, "Texto da prova, irrelevante - o LLM esta mockado neste teste.")

    try:
        with pytest.raises(ErroDeDigitalizacao, match="não corresponde a nenhuma alternativa"):
            digitalizar_prova(
                caminho_prova,
                None,
                carreira_id=registros.carreira_id,
                banca_id=registros.banca_id,
                edital_id=registros.edital_id,
                ano=2026,
            )

        with conectar() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT count(*) FROM fontes_conteudo WHERE edital_id = %s", (registros.edital_id,)
                )
                total = cur.fetchone()
                assert total is not None
                assert total[0] == 0
    finally:
        _limpar_tudo(registros)


@pytest.mark.skipif(
    not os.environ.get("GEMINI_API_KEY"),
    reason="GEMINI_API_KEY não definida — teste de integração real precisa do Gemini de verdade.",
)
def test_integracao_real_digitaliza_prova_pequena_de_ponta_a_ponta(tmp_path: Path) -> None:
    """Prova de exemplo pequena (2 questões), LLM e Postgres reais — cota permitindo.

    Conteúdo representativo (não é cópia de uma prova real), com prova e gabarito no
    mesmo PDF — exercita também o caminho "arquivo único" do pipeline.
    """
    sufixo = str(uuid.uuid4())
    registros = _criar_registros_de_apoio(sufixo)

    texto_prova_e_gabarito = f"""
    PROVA DE CONHECIMENTOS ESPECÍFICOS

    CONHECIMENTOS ESPECÍFICOS - {registros.disciplina_nome}

    1. Segundo a Constituição Federal de 1988, o Brasil se constitui em Estado
    Democrático de Direito.

    2. Assinale a alternativa que apresenta corretamente os Poderes da União,
    conforme a Constituição Federal de 1988.
    A) Executivo, Legislativo e Judiciário.
    B) Apenas Executivo e Legislativo.
    C) Apenas o Judiciário.
    D) Nenhuma das anteriores.

    GABARITO OFICIAL
    Questão 1: Certo
    Questão 2: A
    """

    caminho_prova = tmp_path / "prova_exemplo.pdf"
    _pdf_minimo(caminho_prova, texto_prova_e_gabarito)

    try:
        resultado = digitalizar_prova(
            caminho_prova,
            None,
            carreira_id=registros.carreira_id,
            banca_id=registros.banca_id,
            edital_id=registros.edital_id,
            ano=2026,
        )

        assert not resultado.pulou
        assert resultado.fonte_prova_id is not None
        assert resultado.quantidade_questoes >= 1

        with conectar() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT status, origem, carreira_id, banca_id, ano FROM questoes "
                    "WHERE fonte_prova_id = %s",
                    (resultado.fonte_prova_id,),
                )
                linhas = cur.fetchall()

        assert len(linhas) == resultado.quantidade_questoes
        for status, origem, carreira_id, banca_id, ano in linhas:
            assert status == "em_revisao"
            assert origem == "reproduzida_prova_oficial"
            assert UUID(str(carreira_id)) == registros.carreira_id
            assert UUID(str(banca_id)) == registros.banca_id
            assert ano == 2026
    finally:
        _limpar_tudo(registros)
