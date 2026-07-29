using Forja.Application.Estudo;
using Forja.Domain.Estudo;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class RevisaoEspacadaServiceTests
{
    private readonly Mock<IRevisaoEspacadaRepository> _revisaoRepository = new();
    private readonly RevisaoEspacadaService _service;

    public RevisaoEspacadaServiceTests()
    {
        _service = new RevisaoEspacadaService(_revisaoRepository.Object);
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_PrimeiraRespostaErrada_CriaComIntervaloMinimoEUmErro()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        _revisaoRepository.Setup(r => r.GetByUsuarioIdEQuestaoIdAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RevisaoEspacada?)null);

        var resultado = await _service.RegistrarRespostaAsync(usuarioId, questaoId, correta: false);

        resultado.ErrosConsecutivos.Should().Be(1);
        resultado.IntervaloDiasAtual.Should().Be(1);
        resultado.ProximaRevisaoEm.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
        _revisaoRepository.Verify(r => r.AddAsync(It.IsAny<RevisaoEspacada>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_ErroConsecutivo_ResetaIntervaloParaOMinimo()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        var existente = new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            QuestaoId = questaoId,
            ErrosConsecutivos = 1,
            IntervaloDiasAtual = 8, // ja tinha crescido por acertos anteriores
        };
        _revisaoRepository.Setup(r => r.GetByUsuarioIdEQuestaoIdAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var resultado = await _service.RegistrarRespostaAsync(usuarioId, questaoId, correta: false);

        resultado.ErrosConsecutivos.Should().Be(2);
        resultado.IntervaloDiasAtual.Should().Be(1, "erro consecutivo deve resetar o intervalo para o minimo, revisando mais cedo");
        _revisaoRepository.Verify(r => r.Update(It.IsAny<RevisaoEspacada>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_AcertoAposErros_ZeraErrosEAumentaIntervalo()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        var existente = new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            QuestaoId = questaoId,
            ErrosConsecutivos = 3,
            IntervaloDiasAtual = 1,
        };
        _revisaoRepository.Setup(r => r.GetByUsuarioIdEQuestaoIdAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var resultado = await _service.RegistrarRespostaAsync(usuarioId, questaoId, correta: true);

        resultado.ErrosConsecutivos.Should().Be(0);
        resultado.IntervaloDiasAtual.Should().Be(2, "acerto deve aumentar o intervalo, revisando mais tarde");
        resultado.ProximaRevisaoEm.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2));
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_AcertosConsecutivos_ContinuaDobrandoOIntervalo()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        var existente = new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            QuestaoId = questaoId,
            ErrosConsecutivos = 0,
            IntervaloDiasAtual = 4,
        };
        _revisaoRepository.Setup(r => r.GetByUsuarioIdEQuestaoIdAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        var resultado = await _service.RegistrarRespostaAsync(usuarioId, questaoId, correta: true);

        resultado.IntervaloDiasAtual.Should().Be(8);
    }

    [TestMethod]
    public async Task ObterPendentesAsync_DelegaParaRepositorioComADataDeHoje()
    {
        var usuarioId = Guid.NewGuid();
        var pendentes = new List<RevisaoEspacada> { new() { Id = Guid.NewGuid(), UsuarioId = usuarioId } };
        _revisaoRepository
            .Setup(r => r.GetPendentesAsync(usuarioId, DateOnly.FromDateTime(DateTime.UtcNow), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendentes);

        var resultado = await _service.ObterPendentesAsync(usuarioId);

        resultado.Should().BeEquivalentTo(pendentes);
    }
}
