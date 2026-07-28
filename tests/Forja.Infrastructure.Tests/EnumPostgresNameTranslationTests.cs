using Forja.Domain.Conteudo;
using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using FluentAssertions;
using Npgsql.NameTranslation;

namespace Forja.Infrastructure.Tests;

/// <summary>
/// Valida que o <see cref="NpgsqlSnakeCaseNameTranslator"/> — o mesmo tradutor usado por
/// <c>HasPostgresEnum&lt;TEnum&gt;()</c> no <c>ForjaDbContext</c> — converte cada membro dos enums do
/// Domain exatamente para o valor definido nos <c>CREATE TYPE ... ENUM(...)</c> do Postgres.
/// Cobre especificamente os casos de risco com siglas (GeradaIA, GeradoViaIA).
/// </summary>
[TestClass]
public class EnumPostgresNameTranslationTests
{
    private readonly NpgsqlSnakeCaseNameTranslator _translator = new();

    [TestMethod]
    [DataRow(NivelUsuario.Iniciante, "iniciante")]
    [DataRow(NivelUsuario.Intermediario, "intermediario")]
    [DataRow(NivelUsuario.Avancado, "avancado")]
    public void NivelUsuario_DeveTraduzirParaValorPostgres(NivelUsuario valor, string esperado)
    {
        _translator.TranslateMemberName(valor.ToString()).Should().Be(esperado);
    }

    [TestMethod]
    [DataRow(TipoQuestao.CertoErrado, "certo_errado")]
    [DataRow(TipoQuestao.MultiplaEscolha, "multipla_escolha")]
    public void TipoQuestao_DeveTraduzirParaValorPostgres(TipoQuestao valor, string esperado)
    {
        _translator.TranslateMemberName(valor.ToString()).Should().Be(esperado);
    }

    [TestMethod]
    [DataRow(StatusQuestao.Rascunho, "rascunho")]
    [DataRow(StatusQuestao.EmRevisao, "em_revisao")]
    [DataRow(StatusQuestao.Aprovada, "aprovada")]
    [DataRow(StatusQuestao.Rejeitada, "rejeitada")]
    public void StatusQuestao_DeveTraduzirParaValorPostgres(StatusQuestao valor, string esperado)
    {
        _translator.TranslateMemberName(valor.ToString()).Should().Be(esperado);
    }

    [TestMethod]
    [DataRow(OrigemQuestao.GeradaIA, "gerada_ia")]
    [DataRow(OrigemQuestao.ReproduzidaProvaOficial, "reproduzida_prova_oficial")]
    [DataRow(OrigemQuestao.Inedita, "inedita")]
    public void OrigemQuestao_DeveTraduzirParaValorPostgres(OrigemQuestao valor, string esperado)
    {
        _translator.TranslateMemberName(valor.ToString()).Should().Be(esperado);
    }

    [TestMethod]
    [DataRow(TipoFonte.Lei, "lei")]
    [DataRow(TipoFonte.Edital, "edital")]
    [DataRow(TipoFonte.Prova, "prova")]
    public void TipoFonte_DeveTraduzirParaValorPostgres(TipoFonte valor, string esperado)
    {
        _translator.TranslateMemberName(valor.ToString()).Should().Be(esperado);
    }

    [TestMethod]
    [DataRow(StatusItemPlano.Pendente, "pendente")]
    [DataRow(StatusItemPlano.Concluido, "concluido")]
    public void StatusItemPlano_DeveTraduzirParaValorPostgres(StatusItemPlano valor, string esperado)
    {
        _translator.TranslateMemberName(valor.ToString()).Should().Be(esperado);
    }

    [TestMethod]
    public void GeradoViaIA_NomeDeColuna_DeveTraduzirComoNaoRegressao()
    {
        // PlanoEstudo.GeradoViaIA -> coluna "gerado_via_ia": mesmo caso de risco (sigla dupla no fim)
        // do enum OrigemQuestao.GeradaIA, validado aqui separadamente porque é propriedade, não enum.
        _translator.TranslateMemberName("GeradoViaIA").Should().Be("gerado_via_ia");
    }
}
