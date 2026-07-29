using Forja.Application.Estudo;
using Forja.Domain.Estudo;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class SessaoEstudoServiceTests
{
    private readonly Mock<ISessaoEstudoRepository> _sessaoEstudoRepository = new();
    private readonly SessaoEstudoService _service;

    public SessaoEstudoServiceTests()
    {
        _service = new SessaoEstudoService(_sessaoEstudoRepository.Object);
    }

    [TestMethod]
    public async Task IniciarSessaoAsync_SemSessaoHoje_CriaNova()
    {
        var usuarioId = Guid.NewGuid();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        _sessaoEstudoRepository
            .Setup(r => r.GetByUsuarioIdEDataAsync(usuarioId, hoje, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await _service.IniciarSessaoAsync(usuarioId);

        resultado.UsuarioId.Should().Be(usuarioId);
        resultado.DataSessao.Should().Be(hoje);

        _sessaoEstudoRepository.Verify(r => r.AddAsync(It.Is<SessaoEstudo>(s => s.UsuarioId == usuarioId && s.DataSessao == hoje), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task IniciarSessaoAsync_JaExisteSessaoHoje_RetornaExistenteSemCriarDeNovo()
    {
        var usuarioId = Guid.NewGuid();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var sessaoExistente = new SessaoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, DataSessao = hoje };
        _sessaoEstudoRepository
            .Setup(r => r.GetByUsuarioIdEDataAsync(usuarioId, hoje, It.IsAny<CancellationToken>()))
            .ReturnsAsync([sessaoExistente]);

        var resultado = await _service.IniciarSessaoAsync(usuarioId);

        resultado.Should().BeSameAs(sessaoExistente);
        _sessaoEstudoRepository.Verify(r => r.AddAsync(It.IsAny<SessaoEstudo>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
