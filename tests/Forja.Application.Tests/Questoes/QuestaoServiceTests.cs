using Forja.Application.Common;
using Forja.Application.Questoes;
using Forja.Domain.Questoes;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Questoes;

[TestClass]
public class QuestaoServiceTests
{
    private readonly Mock<IQuestaoRepository> _questaoRepository = new();
    private readonly QuestaoService _service;

    public QuestaoServiceTests()
    {
        _service = new QuestaoService(_questaoRepository.Object);
    }

    [TestMethod]
    public async Task BuscarAsync_FiltroPorCarreiraSozinha_PassaBancaEDisciplinaNulosParaORepositorio()
    {
        var carreiraId = Guid.NewGuid();
        var esperado = new List<Questao> { new() { Id = Guid.NewGuid(), CarreiraId = carreiraId } };

        _questaoRepository
            .Setup(r => r.GetByFiltroAsync(carreiraId, null, null, StatusQuestao.Aprovada, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var resultado = await _service.BuscarAsync(carreiraId, null, null);

        resultado.Should().BeEquivalentTo(esperado);
        _questaoRepository.Verify(
            r => r.GetByFiltroAsync(carreiraId, null, null, StatusQuestao.Aprovada, 0, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task BuscarAsync_FiltroPorBancaSozinha_PassaCarreiraEDisciplinaNulosParaORepositorio()
    {
        var bancaId = Guid.NewGuid();
        var esperado = new List<Questao> { new() { Id = Guid.NewGuid(), BancaId = bancaId } };

        _questaoRepository
            .Setup(r => r.GetByFiltroAsync(null, bancaId, null, StatusQuestao.Aprovada, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var resultado = await _service.BuscarAsync(null, bancaId, null);

        resultado.Should().BeEquivalentTo(esperado);
        _questaoRepository.Verify(
            r => r.GetByFiltroAsync(null, bancaId, null, StatusQuestao.Aprovada, 0, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task BuscarAsync_FiltroPorCarreiraEBancaJuntos_PassaOsDoisFiltrosParaORepositorio()
    {
        var carreiraId = Guid.NewGuid();
        var bancaId = Guid.NewGuid();
        var esperado = new List<Questao> { new() { Id = Guid.NewGuid(), CarreiraId = carreiraId, BancaId = bancaId } };

        _questaoRepository
            .Setup(r => r.GetByFiltroAsync(carreiraId, bancaId, null, StatusQuestao.Aprovada, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperado);

        var resultado = await _service.BuscarAsync(carreiraId, bancaId, null);

        resultado.Should().BeEquivalentTo(esperado);
        _questaoRepository.Verify(
            r => r.GetByFiltroAsync(carreiraId, bancaId, null, StatusQuestao.Aprovada, 0, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ObterPorIdAsync_QuestaoNaoAprovada_LancaQuestaoNaoEncontrada()
    {
        var id = Guid.NewGuid();
        _questaoRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Questao { Id = id, Status = StatusQuestao.Rascunho });

        var acao = () => _service.ObterPorIdAsync(id);

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task ObterPorIdAsync_QuestaoInexistente_LancaQuestaoNaoEncontrada()
    {
        var id = Guid.NewGuid();
        _questaoRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Questao?)null);

        var acao = () => _service.ObterPorIdAsync(id);

        await acao.Should().ThrowAsync<NotFoundException>();
    }
}
