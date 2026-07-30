using Forja.Application.Common;
using Forja.Application.Contribuicao;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Contribuicao;
using Forja.Domain.Gamificacao;
using Forja.Domain.Usuarios;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Contribuicao;

[TestClass]
public class ContribuicaoServiceTests
{
    private readonly Mock<IContribuicaoConteudoRepository> _contribuicaoRepository = new();
    private readonly Mock<ITopicoRepository> _topicoRepository = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IReputacaoContribuicaoRepository> _reputacaoRepository = new();
    private readonly Mock<IUsuarioMedalhaRepository> _usuarioMedalhaRepository = new();
    private readonly Mock<IAdminAuthorizer> _adminAuthorizer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ContribuicaoService _service;

    public ContribuicaoServiceTests()
    {
        _service = new ContribuicaoService(
            _contribuicaoRepository.Object,
            _topicoRepository.Object,
            _usuarioRepository.Object,
            _reputacaoRepository.Object,
            _usuarioMedalhaRepository.Object,
            _adminAuthorizer.Object,
            _unitOfWork.Object);
    }

    private static Usuario CriarUsuario(string email) => new() { Id = Guid.NewGuid(), Email = email, Nome = "Usuário" };

    private static ContribuicaoConteudo CriarContribuicao(Guid usuarioId) => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = usuarioId,
        TopicoId = Guid.NewGuid(),
        Tipo = TipoContribuicao.Video,
        Link = "https://exemplo.com/video",
        Status = StatusContribuicao.EmRevisao,
        CriadoEm = DateTimeOffset.UtcNow,
    };

    [TestMethod]
    public async Task SubmeterAsync_TopicoInexistente_LancaNotFoundException()
    {
        _topicoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Topico?)null);

        var acao = () => _service.SubmeterAsync(Guid.NewGuid(), Guid.NewGuid(), TipoContribuicao.Pdf, "https://exemplo.com/a.pdf");

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task SubmeterAsync_LinkInvalido_LancaArgumentException()
    {
        _topicoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Topico { Id = Guid.NewGuid(), Nome = "Tópico" });

        var acao = () => _service.SubmeterAsync(Guid.NewGuid(), Guid.NewGuid(), TipoContribuicao.Pdf, "não-é-uma-url");

        await acao.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task SubmeterAsync_ValidaComEmRevisao()
    {
        _topicoRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Topico { Id = Guid.NewGuid(), Nome = "Tópico" });

        var contribuicao = await _service.SubmeterAsync(Guid.NewGuid(), Guid.NewGuid(), TipoContribuicao.Video, "https://exemplo.com/video");

        contribuicao.Status.Should().Be(StatusContribuicao.EmRevisao);
        _contribuicaoRepository.Verify(r => r.AddAsync(It.IsAny<ContribuicaoConteudo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task AprovarAsync_ModeradorNaoAdmin_LancaNaoAutorizadoExceptionENaoAlteraNada()
    {
        var moderador = CriarUsuario("nao-admin@exemplo.com");
        _usuarioRepository.Setup(r => r.GetByIdAsync(moderador.Id, It.IsAny<CancellationToken>())).ReturnsAsync(moderador);
        _adminAuthorizer.Setup(a => a.EhAdmin(moderador.Email)).Returns(false);

        var acao = () => _service.AprovarAsync(Guid.NewGuid(), moderador.Id);

        await acao.Should().ThrowAsync<NaoAutorizadoException>();
        _contribuicaoRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task AprovarAsync_PrimeiraAprovacaoDoAutor_ConcedePontosEMedalhaDeMarco()
    {
        var admin = CriarUsuario("admin@exemplo.com");
        var autorId = Guid.NewGuid();
        var contribuicao = CriarContribuicao(autorId);

        _usuarioRepository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _adminAuthorizer.Setup(a => a.EhAdmin(admin.Email)).Returns(true);
        _contribuicaoRepository.Setup(r => r.GetByIdAsync(contribuicao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contribuicao);
        _reputacaoRepository.Setup(r => r.GetByIdAsync(autorId, It.IsAny<CancellationToken>())).ReturnsAsync((ReputacaoContribuicao?)null);
        _usuarioMedalhaRepository.Setup(r => r.ExisteAsync(autorId, MedalhasConhecidas.PrimeiraContribuicaoAprovadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await _service.AprovarAsync(contribuicao.Id, admin.Id);

        resultado.Status.Should().Be(StatusContribuicao.Aprovada);
        resultado.ModeradoPor.Should().Be(admin.Id);
        // AddAsync já deve deixar a entidade com os pontos corretos — Update() não deve ser chamado
        // em cima de uma entidade recém-adicionada (isso reclassificaria Added -> Modified e geraria
        // um UPDATE pra uma linha que ainda não existe).
        _reputacaoRepository.Verify(r => r.AddAsync(It.Is<ReputacaoContribuicao>(rc => rc.UsuarioId == autorId && rc.PontosContribuicao == 10), It.IsAny<CancellationToken>()), Times.Once);
        _reputacaoRepository.Verify(r => r.Update(It.IsAny<ReputacaoContribuicao>()), Times.Never);
        _usuarioMedalhaRepository.Verify(
            r => r.AddAsync(It.Is<UsuarioMedalha>(um => um.UsuarioId == autorId && um.MedalhaId == MedalhasConhecidas.PrimeiraContribuicaoAprovadaId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task AprovarAsync_AutorJaTemAMedalhaDeMarco_NaoConcedeDeNovo()
    {
        var admin = CriarUsuario("admin@exemplo.com");
        var autorId = Guid.NewGuid();
        var contribuicao = CriarContribuicao(autorId);
        var reputacaoExistente = new ReputacaoContribuicao { UsuarioId = autorId, PontosContribuicao = 30 };

        _usuarioRepository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _adminAuthorizer.Setup(a => a.EhAdmin(admin.Email)).Returns(true);
        _contribuicaoRepository.Setup(r => r.GetByIdAsync(contribuicao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contribuicao);
        _reputacaoRepository.Setup(r => r.GetByIdAsync(autorId, It.IsAny<CancellationToken>())).ReturnsAsync(reputacaoExistente);
        _usuarioMedalhaRepository.Setup(r => r.ExisteAsync(autorId, MedalhasConhecidas.PrimeiraContribuicaoAprovadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.AprovarAsync(contribuicao.Id, admin.Id);

        _usuarioMedalhaRepository.Verify(r => r.AddAsync(It.IsAny<UsuarioMedalha>(), It.IsAny<CancellationToken>()), Times.Never);
        _reputacaoRepository.Verify(r => r.Update(It.Is<ReputacaoContribuicao>(rc => rc.PontosContribuicao == 40)), Times.Once);
    }

    [TestMethod]
    public async Task RejeitarAsync_ModeradorAdmin_MarcaStatusSemAfetarReputacao()
    {
        var admin = CriarUsuario("admin@exemplo.com");
        var contribuicao = CriarContribuicao(Guid.NewGuid());

        _usuarioRepository.Setup(r => r.GetByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        _adminAuthorizer.Setup(a => a.EhAdmin(admin.Email)).Returns(true);
        _contribuicaoRepository.Setup(r => r.GetByIdAsync(contribuicao.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contribuicao);

        var resultado = await _service.RejeitarAsync(contribuicao.Id, admin.Id);

        resultado.Status.Should().Be(StatusContribuicao.Rejeitada);
        _reputacaoRepository.Verify(r => r.AddAsync(It.IsAny<ReputacaoContribuicao>(), It.IsAny<CancellationToken>()), Times.Never);
        _usuarioMedalhaRepository.Verify(r => r.AddAsync(It.IsAny<UsuarioMedalha>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
