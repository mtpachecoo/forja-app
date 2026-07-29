using Forja.Application.Estudo;
using Forja.Application.Gamificacao;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class IniciarSessaoComEfeitosServiceTests
{
    private readonly Mock<ISessaoEstudoService> _sessaoEstudoService = new();
    private readonly Mock<IStreakService> _streakService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IniciarSessaoComEfeitosService _service;

    public IniciarSessaoComEfeitosServiceTests()
    {
        _service = new IniciarSessaoComEfeitosService(_sessaoEstudoService.Object, _streakService.Object, _unitOfWork.Object);
    }

    [TestMethod]
    public async Task IniciarAsync_ChamaSessaoEstudoServiceEDepoisStreakService()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoEsperada = new SessaoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, DataSessao = DateOnly.FromDateTime(DateTime.UtcNow) };

        _sessaoEstudoService.Setup(s => s.IniciarSessaoAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(sessaoEsperada);
        _streakService.Setup(s => s.RegistrarAtividadeAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(new Streak { UsuarioId = usuarioId });

        var resultado = await _service.IniciarAsync(usuarioId);

        resultado.Should().BeSameAs(sessaoEsperada);
        _streakService.Verify(s => s.RegistrarAtividadeAsync(usuarioId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task IniciarAsync_RespeitaAOrdem_StreakSoEhChamadoDepoisDaSessao()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoEsperada = new SessaoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId };
        var ordem = new List<string>();

        _sessaoEstudoService
            .Setup(s => s.IniciarSessaoAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessaoEsperada)
            .Callback(() => ordem.Add("sessao"));
        _streakService
            .Setup(s => s.RegistrarAtividadeAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Streak { UsuarioId = usuarioId })
            .Callback(() => ordem.Add("streak"));
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .Callback(() => ordem.Add("savechanges"));

        await _service.IniciarAsync(usuarioId);

        ordem.Should().Equal("sessao", "streak", "savechanges");
    }

    [TestMethod]
    public async Task IniciarAsync_QuandoStreakFalha_NaoDeixaSessaoPersistidaSozinha()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoEsperada = new SessaoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId };

        _sessaoEstudoService.Setup(s => s.IniciarSessaoAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(sessaoEsperada);
        _streakService
            .Setup(s => s.RegistrarAtividadeAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha simulada no streak"));

        var acao = () => _service.IniciarAsync(usuarioId);

        await acao.Should().ThrowAsync<InvalidOperationException>();

        // SessaoEstudoService já rodou (enfileirou a sessão no repositório), mas como o streak falhou
        // antes do único SaveChangesAsync deste orquestrador, nada foi efetivamente persistido — a
        // sessão nunca fica salva sem o streak atualizado.
        _sessaoEstudoService.Verify(s => s.IniciarSessaoAsync(usuarioId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
