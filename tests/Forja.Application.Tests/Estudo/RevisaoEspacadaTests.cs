using Forja.Domain.Estudo;
using FluentAssertions;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class RevisaoEspacadaTests
{
    [TestMethod]
    public void RegistrarResultado_PrimeiraRespostaErrada_MantemIntervaloMinimoEUmErro()
    {
        var revisao = new RevisaoEspacada { Id = Guid.NewGuid() };

        revisao.RegistrarResultado(correta: false, new DateOnly(2026, 7, 27));

        revisao.ErrosConsecutivos.Should().Be(1);
        revisao.IntervaloDiasAtual.Should().Be(1);
        revisao.ProximaRevisaoEm.Should().Be(new DateOnly(2026, 7, 28));
    }

    [TestMethod]
    public void RegistrarResultado_ErroConsecutivo_ResetaIntervaloParaOMinimo()
    {
        var revisao = new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            ErrosConsecutivos = 1,
            IntervaloDiasAtual = 8, // já tinha crescido por acertos anteriores
        };

        revisao.RegistrarResultado(correta: false, new DateOnly(2026, 7, 27));

        revisao.ErrosConsecutivos.Should().Be(2);
        revisao.IntervaloDiasAtual.Should().Be(1, "erro consecutivo deve resetar o intervalo para o mínimo, revisando mais cedo");
    }

    [TestMethod]
    public void RegistrarResultado_AcertoAposErros_ZeraErrosEDobraOIntervalo()
    {
        var revisao = new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            ErrosConsecutivos = 3,
            IntervaloDiasAtual = 1,
        };

        revisao.RegistrarResultado(correta: true, new DateOnly(2026, 7, 27));

        revisao.ErrosConsecutivos.Should().Be(0);
        revisao.IntervaloDiasAtual.Should().Be(2, "acerto deve aumentar o intervalo, revisando mais tarde");
        revisao.ProximaRevisaoEm.Should().Be(new DateOnly(2026, 7, 29));
    }

    [TestMethod]
    public void RegistrarResultado_AcertosConsecutivos_ContinuaDobrandoOIntervalo()
    {
        var revisao = new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            ErrosConsecutivos = 0,
            IntervaloDiasAtual = 4,
        };

        revisao.RegistrarResultado(correta: true, new DateOnly(2026, 7, 27));

        revisao.IntervaloDiasAtual.Should().Be(8);
    }
}
