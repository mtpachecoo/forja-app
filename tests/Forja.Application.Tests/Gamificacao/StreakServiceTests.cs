using Forja.Application.Gamificacao;
using Forja.Domain.Gamificacao;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Gamificacao;

[TestClass]
public class StreakServiceTests
{
    private readonly Mock<IStreakRepository> _streakRepository = new();
    private readonly StreakService _service;

    public StreakServiceTests()
    {
        _service = new StreakService(_streakRepository.Object);
    }

    [TestMethod]
    public async Task RegistrarAtividadeAsync_PrimeiraAtividade_ComecaComUmDia()
    {
        var usuarioId = Guid.NewGuid();
        _streakRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync((Streak?)null);

        var resultado = await _service.RegistrarAtividadeAsync(usuarioId);

        resultado.DiasConsecutivos.Should().Be(1);
    }

    [TestMethod]
    public async Task RegistrarAtividadeAsync_AtividadeOntem_IncrementaSequencia()
    {
        var usuarioId = Guid.NewGuid();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        _streakRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Streak { UsuarioId = usuarioId, DiasConsecutivos = 4, UltimaAtividadeEm = hoje.AddDays(-1) });

        var resultado = await _service.RegistrarAtividadeAsync(usuarioId);

        resultado.DiasConsecutivos.Should().Be(5);
    }

    [TestMethod]
    public async Task RegistrarAtividadeAsync_LacunaDeDias_ReiniciaSequencia()
    {
        var usuarioId = Guid.NewGuid();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        _streakRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Streak { UsuarioId = usuarioId, DiasConsecutivos = 10, UltimaAtividadeEm = hoje.AddDays(-3) });

        var resultado = await _service.RegistrarAtividadeAsync(usuarioId);

        resultado.DiasConsecutivos.Should().Be(1);
    }

    [TestMethod]
    public async Task RegistrarAtividadeAsync_JaRegistradoHoje_NaoMudaNada()
    {
        var usuarioId = Guid.NewGuid();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        _streakRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Streak { UsuarioId = usuarioId, DiasConsecutivos = 4, UltimaAtividadeEm = hoje });

        var resultado = await _service.RegistrarAtividadeAsync(usuarioId);

        resultado.DiasConsecutivos.Should().Be(4);
        _streakRepository.Verify(r => r.Update(It.IsAny<Streak>()), Times.Never);
    }
}
