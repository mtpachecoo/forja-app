using Forja.Domain.Questoes;
using Forja.Infrastructure.Conversions;
using FluentAssertions;

namespace Forja.Infrastructure.Tests.Conversions;

[TestClass]
public class AlternativasValueComparerTests
{
    private readonly AlternativasValueComparer _comparer = new();

    [TestMethod]
    public void Equals_ListasComMesmoConteudoNaMesmaOrdem_RetornaTrue()
    {
        var a = new List<Alternativa> { new("A", "Primeira"), new("B", "Segunda") };
        var b = new List<Alternativa> { new("A", "Primeira"), new("B", "Segunda") };

        _comparer.Equals(a, b).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_ListasComMesmoConteudoEmOrdemDiferente_RetornaFalse()
    {
        // Decisão: o comparador é sensível à ordem (usa SequenceEqual) — a ordem das alternativas
        // é significativa pro change tracker do EF Core detectar uma reordenação como mudança real.
        var a = new List<Alternativa> { new("A", "Primeira"), new("B", "Segunda") };
        var b = new List<Alternativa> { new("B", "Segunda"), new("A", "Primeira") };

        _comparer.Equals(a, b).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_UmaListaNulaEOutraNao_RetornaFalse()
    {
        var a = new List<Alternativa> { new("A", "Primeira") };

        _comparer.Equals(a, null).Should().BeFalse();
        _comparer.Equals(null, a).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_AmbasAsListasNulas_RetornaTrue()
    {
        _comparer.Equals(null, null).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_ListasDeTamanhosDiferentes_RetornaFalse()
    {
        var a = new List<Alternativa> { new("A", "Primeira") };
        var b = new List<Alternativa> { new("A", "Primeira"), new("B", "Segunda") };

        _comparer.Equals(a, b).Should().BeFalse();
    }
}
