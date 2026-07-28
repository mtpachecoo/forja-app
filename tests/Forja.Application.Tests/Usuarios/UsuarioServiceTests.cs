using System.Security.Claims;
using Forja.Application.Usuarios;
using Forja.Domain.Common;
using Forja.Domain.Usuarios;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Usuarios;

[TestClass]
public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IIdentidadeExternaRepository> _identidadeExternaRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _service = new UsuarioService(_usuarioRepository.Object, _identidadeExternaRepository.Object, _unitOfWork.Object);
    }

    private static ClaimsPrincipal CriarPrincipal(string? sub)
    {
        var claims = sub == null ? [] : new[] { new Claim("sub", sub) };
        var identity = new ClaimsIdentity(claims, "Bearer");
        return new ClaimsPrincipal(identity);
    }

    [TestMethod]
    public async Task ResolverUsuarioAutenticadoAsync_UsuarioNovo_CriaEPersisteORegistro()
    {
        var usuarioId = Guid.NewGuid();
        var principal = CriarPrincipal(usuarioId.ToString());

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        _identidadeExternaRepository
            .Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentidadeExterna(usuarioId, "Fulano de Tal", "fulano@example.com"));

        var resultado = await _service.ResolverUsuarioAutenticadoAsync(principal);

        resultado.Id.Should().Be(usuarioId);
        resultado.Nome.Should().Be("Fulano de Tal");
        resultado.Email.Should().Be("fulano@example.com");
        resultado.Nivel.Should().Be(NivelUsuario.Iniciante);
        resultado.TempoDisponivelMinDia.Should().Be(60);

        _usuarioRepository.Verify(r => r.AddAsync(It.Is<Usuario>(u => u.Id == usuarioId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ResolverUsuarioAutenticadoAsync_UsuarioExistente_RetornaSemPersistir()
    {
        var usuarioId = Guid.NewGuid();
        var principal = CriarPrincipal(usuarioId.ToString());
        var usuarioExistente = new Usuario
        {
            Id = usuarioId,
            Nome = "Já Cadastrado",
            Email = "existente@example.com",
            Nivel = NivelUsuario.Avancado,
            TempoDisponivelMinDia = 120,
        };

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioExistente);

        var resultado = await _service.ResolverUsuarioAutenticadoAsync(principal);

        resultado.Should().BeSameAs(usuarioExistente);

        _usuarioRepository.Verify(r => r.AddAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ResolverUsuarioAutenticadoAsync_SemClaimSub_LancaUsuarioNaoAutenticado()
    {
        var principal = CriarPrincipal(null);

        var acao = () => _service.ResolverUsuarioAutenticadoAsync(principal);

        await acao.Should().ThrowAsync<UsuarioNaoAutenticadoException>();
    }

    [TestMethod]
    public async Task ResolverUsuarioAutenticadoAsync_ClaimSubNaoEhGuid_LancaUsuarioNaoAutenticado()
    {
        var principal = CriarPrincipal("nao-e-um-guid");

        var acao = () => _service.ResolverUsuarioAutenticadoAsync(principal);

        await acao.Should().ThrowAsync<UsuarioNaoAutenticadoException>();
    }

    [TestMethod]
    public async Task ResolverUsuarioAutenticadoAsync_SemIdentidadeExterna_LancaUsuarioNaoAutenticado()
    {
        var usuarioId = Guid.NewGuid();
        var principal = CriarPrincipal(usuarioId.ToString());

        _usuarioRepository
            .Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        _identidadeExternaRepository
            .Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentidadeExterna?)null);

        var acao = () => _service.ResolverUsuarioAutenticadoAsync(principal);

        await acao.Should().ThrowAsync<UsuarioNaoAutenticadoException>();
    }
}
