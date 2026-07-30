using Forja.Domain.Gamificacao;
using FluentAssertions;

namespace Forja.Application.Tests.Gamificacao;

[TestClass]
public class ReputacaoCalculatorTests
{
    [TestMethod]
    [DataRow(0, NivelReputacao.Bronze)]
    [DataRow(49, NivelReputacao.Bronze)]
    [DataRow(50, NivelReputacao.Prata)]
    [DataRow(149, NivelReputacao.Prata)]
    [DataRow(150, NivelReputacao.Ouro)]
    [DataRow(399, NivelReputacao.Ouro)]
    [DataRow(400, NivelReputacao.Platina)]
    [DataRow(10_000, NivelReputacao.Platina)]
    public void CalcularNivel_NosLimiares_RetornaNivelEsperado(int pontos, NivelReputacao esperado)
    {
        ReputacaoCalculator.CalcularNivel(pontos).Should().Be(esperado);
    }

    [TestMethod]
    public void CalcularNivel_PontosNegativos_TratadoComoZero()
    {
        ReputacaoCalculator.CalcularNivel(-100).Should().Be(NivelReputacao.Bronze);
    }
}
