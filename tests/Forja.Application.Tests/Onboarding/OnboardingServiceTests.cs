using Forja.Application.Estudo;
using Forja.Application.Onboarding;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Usuarios;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Onboarding;

[TestClass]
public class OnboardingServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IPlanoEstudoService> _planoEstudoService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly OnboardingService _service;

    public OnboardingServiceTests()
    {
        _service = new OnboardingService(_usuarioRepository.Object, _planoEstudoService.Object, _unitOfWork.Object);
    }

    private static PlanoGerado CriarPlanoGerado(Guid carreiraId) => new(
        new PlanoEstudo { Id = Guid.NewGuid(), CarreiraId = carreiraId, CriadoEm = DateTimeOffset.UtcNow },
        []);

    [TestMethod]
    public async Task CompletarOnboardingAsync_UsuarioSemDadosPrevios_AtualizaUsuarioComOsValoresInformados()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var usuario = new Usuario { Id = usuarioId, Nivel = NivelUsuario.Iniciante, TempoDisponivelMinDia = 60 };
        _usuarioRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _planoEstudoService
            .Setup(s => s.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, 90, NivelUsuario.Avancado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarPlanoGerado(carreiraId));

        var resultado = await _service.CompletarOnboardingAsync(usuarioId, carreiraId, 90, NivelUsuario.Avancado);

        resultado.Usuario.TempoDisponivelMinDia.Should().Be(90);
        resultado.Usuario.Nivel.Should().Be(NivelUsuario.Avancado);
        _usuarioRepository.Verify(r => r.Update(It.Is<Usuario>(u => u.TempoDisponivelMinDia == 90 && u.Nivel == NivelUsuario.Avancado)), Times.Once);
    }

    [TestMethod]
    public async Task CompletarOnboardingAsync_ChamaGeracaoDePlanoComOContextoInformado_NaoComOsValoresAntigos()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var usuario = new Usuario { Id = usuarioId, Nivel = NivelUsuario.Iniciante, TempoDisponivelMinDia = 30 };
        _usuarioRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _planoEstudoService
            .Setup(s => s.ObterOuGerarPlanoAtualAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<NivelUsuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarPlanoGerado(carreiraId));

        await _service.CompletarOnboardingAsync(usuarioId, carreiraId, 120, NivelUsuario.Intermediario);

        _planoEstudoService.Verify(
            s => s.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, 120, NivelUsuario.Intermediario, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CompletarOnboardingAsync_ReOnboardingComPlanoJaExistente_AindaAssimSalvaAAtualizacaoDoUsuario()
    {
        // ObterOuGerarPlanoAtualAsync não persiste nada quando reaproveita um plano existente — o
        // SaveChangesAsync do OnboardingService precisa acontecer de qualquer forma, senão a
        // atualização de Nivel/TempoDisponivelMinDia feita antes se perde.
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var usuario = new Usuario { Id = usuarioId, Nivel = NivelUsuario.Iniciante, TempoDisponivelMinDia = 30 };
        _usuarioRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _planoEstudoService
            .Setup(s => s.ObterOuGerarPlanoAtualAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<NivelUsuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarPlanoGerado(carreiraId));

        await _service.CompletarOnboardingAsync(usuarioId, carreiraId, 45, NivelUsuario.Avancado);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CompletarOnboardingAsync_UsuarioInexistente_LancaNotFoundException()
    {
        var usuarioId = Guid.NewGuid();
        _usuarioRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var acao = () => _service.CompletarOnboardingAsync(usuarioId, Guid.NewGuid(), 60, NivelUsuario.Iniciante);

        await acao.Should().ThrowAsync<Forja.Application.Common.NotFoundException>();
    }
}
