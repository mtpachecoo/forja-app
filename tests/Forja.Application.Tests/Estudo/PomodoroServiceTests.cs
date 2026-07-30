using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class PomodoroServiceTests
{
    private readonly Mock<ISessaoEstudoRepository> _sessaoEstudoRepository = new();
    private readonly Mock<IPomodoroRepository> _pomodoroRepository = new();
    private readonly Mock<IRespostaUsuarioRepository> _respostaRepository = new();
    private readonly Mock<IPontuacaoRepository> _pontuacaoRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PomodoroService _service;

    public PomodoroServiceTests()
    {
        _service = new PomodoroService(
            _sessaoEstudoRepository.Object,
            _pomodoroRepository.Object,
            _respostaRepository.Object,
            _pontuacaoRepository.Object,
            _unitOfWork.Object);
    }

    private SessaoEstudo ConfigurarSessaoDoUsuario(Guid usuarioId, Guid sessaoId)
    {
        var sessao = new SessaoEstudo { Id = sessaoId, UsuarioId = usuarioId, DataSessao = DateOnly.FromDateTime(DateTime.UtcNow) };
        _sessaoEstudoRepository.Setup(r => r.GetByIdAsync(sessaoId, It.IsAny<CancellationToken>())).ReturnsAsync(sessao);
        return sessao;
    }

    [TestMethod]
    public async Task FinalizarPomodoroAsync_CicloComRespostaRegistrada_Pontua()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoId = Guid.NewGuid();
        var pomodoroId = Guid.NewGuid();

        ConfigurarSessaoDoUsuario(usuarioId, sessaoId);
        _pomodoroRepository.Setup(r => r.GetByIdAsync(pomodoroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pomodoro { Id = pomodoroId, SessaoId = sessaoId, IniciadoEm = DateTimeOffset.UtcNow.AddMinutes(-25) });
        _respostaRepository.Setup(r => r.ContarRespostasNoPomodoroAsync(pomodoroId, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _pontuacaoRepository
            .Setup(r => r.IncrementarPontosAsync(usuarioId, It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid uid, int pontos, DateOnly _, CancellationToken _) => new Pontuacao { UsuarioId = uid, PontosTotal = pontos, PontosSemanaAtual = pontos });

        var resultado = await _service.FinalizarPomodoroAsync(usuarioId, sessaoId, pomodoroId);

        resultado.Pomodoro.QtdRespostasNoCiclo.Should().Be(3);
        resultado.Pomodoro.PontosConcedidos.Should().BeGreaterThan(0);
        resultado.Pontuacao.Should().NotBeNull();
        resultado.Pontuacao!.PontosTotal.Should().Be(resultado.Pomodoro.PontosConcedidos);

        _pontuacaoRepository.Verify(r => r.IncrementarPontosAsync(usuarioId, resultado.Pomodoro.PontosConcedidos, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task FinalizarPomodoroAsync_CicloSemNenhumaResposta_NaoPontuaMesmoCompleto()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoId = Guid.NewGuid();
        var pomodoroId = Guid.NewGuid();

        ConfigurarSessaoDoUsuario(usuarioId, sessaoId);
        // Pomodoro "completo" — 25 minutos corridos desde o inicio — mas sem nenhuma resposta.
        _pomodoroRepository.Setup(r => r.GetByIdAsync(pomodoroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pomodoro { Id = pomodoroId, SessaoId = sessaoId, IniciadoEm = DateTimeOffset.UtcNow.AddMinutes(-25), DuracaoPrevistaMin = 25 });
        _respostaRepository.Setup(r => r.ContarRespostasNoPomodoroAsync(pomodoroId, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var resultado = await _service.FinalizarPomodoroAsync(usuarioId, sessaoId, pomodoroId);

        resultado.Pomodoro.QtdRespostasNoCiclo.Should().Be(0);
        resultado.Pomodoro.PontosConcedidos.Should().Be(0);
        resultado.Pontuacao.Should().BeNull();

        _pontuacaoRepository.Verify(r => r.IncrementarPontosAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task FinalizarPomodoroAsync_JaFinalizadoAnteriormente_NaoConcedePontosDeNovo()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoId = Guid.NewGuid();
        var pomodoroId = Guid.NewGuid();

        ConfigurarSessaoDoUsuario(usuarioId, sessaoId);
        _pomodoroRepository.Setup(r => r.GetByIdAsync(pomodoroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pomodoro { Id = pomodoroId, SessaoId = sessaoId, FinalizadoEm = DateTimeOffset.UtcNow.AddMinutes(-1), PontosConcedidos = 5 });

        var resultado = await _service.FinalizarPomodoroAsync(usuarioId, sessaoId, pomodoroId);

        resultado.Pontuacao.Should().BeNull();
        _respostaRepository.Verify(r => r.ContarRespostasNoPomodoroAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task FinalizarPomodoroAsync_SessaoDeOutroUsuario_LancaNotFound()
    {
        var usuarioId = Guid.NewGuid();
        var outroUsuarioId = Guid.NewGuid();
        var sessaoId = Guid.NewGuid();
        var pomodoroId = Guid.NewGuid();

        ConfigurarSessaoDoUsuario(outroUsuarioId, sessaoId);

        var acao = () => _service.FinalizarPomodoroAsync(usuarioId, sessaoId, pomodoroId);

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task IniciarPomodoroAsync_SessaoValida_CriaPomodoro()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoId = Guid.NewGuid();
        ConfigurarSessaoDoUsuario(usuarioId, sessaoId);

        var pomodoro = await _service.IniciarPomodoroAsync(usuarioId, sessaoId);

        pomodoro.SessaoId.Should().Be(sessaoId);
        pomodoro.FinalizadoEm.Should().BeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
