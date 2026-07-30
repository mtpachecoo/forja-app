using Forja.Application.Home;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Home;

[TestClass]
public class HomeServiceTests
{
    private readonly Mock<IStreakRepository> _streakRepository = new();
    private readonly Mock<IPontuacaoRepository> _pontuacaoRepository = new();
    private readonly Mock<IPlanoEstudoRepository> _planoEstudoRepository = new();
    private readonly Mock<IPlanoItemRepository> _planoItemRepository = new();
    private readonly Mock<IRespostaUsuarioRepository> _respostaUsuarioRepository = new();
    private readonly HomeService _service;

    public HomeServiceTests()
    {
        _service = new HomeService(
            _streakRepository.Object,
            _pontuacaoRepository.Object,
            _planoEstudoRepository.Object,
            _planoItemRepository.Object,
            _respostaUsuarioRepository.Object);
    }

    private void ConfigurarPadraoSemDados(Guid usuarioId)
    {
        _streakRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync((Streak?)null);
        _pontuacaoRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync((Pontuacao?)null);
        _planoEstudoRepository.Setup(r => r.GetByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _respostaUsuarioRepository.Setup(r => r.ContarRespostasNoDiaAsync(usuarioId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _respostaUsuarioRepository.Setup(r => r.GetUltimasAsync(usuarioId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    [TestMethod]
    public async Task ObterResumoAsync_UsuarioSemNenhumDadoAinda_RetornaZerosENuloSemLancar()
    {
        var usuarioId = Guid.NewGuid();
        ConfigurarPadraoSemDados(usuarioId);

        var resumo = await _service.ObterResumoAsync(usuarioId, 10);

        resumo.DiasConsecutivos.Should().Be(0);
        resumo.PontosTotal.Should().Be(0);
        resumo.PontosSemanaAtual.Should().Be(0);
        resumo.PercentualPlanoConcluido.Should().BeNull();
        resumo.RespostasHoje.Should().Be(0);
        resumo.AtividadesRecentes.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ObterResumoAsync_AgregaStreakEPontuacaoExistentes()
    {
        var usuarioId = Guid.NewGuid();
        ConfigurarPadraoSemDados(usuarioId);
        _streakRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Streak { UsuarioId = usuarioId, DiasConsecutivos = 7 });
        _pontuacaoRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pontuacao { UsuarioId = usuarioId, PontosTotal = 250, PontosSemanaAtual = 40 });

        var resumo = await _service.ObterResumoAsync(usuarioId, 10);

        resumo.DiasConsecutivos.Should().Be(7);
        resumo.PontosTotal.Should().Be(250);
        resumo.PontosSemanaAtual.Should().Be(40);
    }

    [TestMethod]
    public async Task ObterResumoAsync_ComPlanoParcialmenteConcluido_CalculaPercentualCorreto()
    {
        var usuarioId = Guid.NewGuid();
        ConfigurarPadraoSemDados(usuarioId);

        var planoAntigo = new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, CriadoEm = DateTimeOffset.UtcNow.AddDays(-10) };
        var planoRecente = new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, CriadoEm = DateTimeOffset.UtcNow };
        _planoEstudoRepository.Setup(r => r.GetByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([planoAntigo, planoRecente]);

        _planoItemRepository.Setup(r => r.GetByPlanoIdAsync(planoRecente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlanoItem { Id = Guid.NewGuid(), PlanoId = planoRecente.Id, Status = StatusItemPlano.Concluido },
                new PlanoItem { Id = Guid.NewGuid(), PlanoId = planoRecente.Id, Status = StatusItemPlano.Concluido },
                new PlanoItem { Id = Guid.NewGuid(), PlanoId = planoRecente.Id, Status = StatusItemPlano.Pendente },
                new PlanoItem { Id = Guid.NewGuid(), PlanoId = planoRecente.Id, Status = StatusItemPlano.Pendente },
            ]);

        var resumo = await _service.ObterResumoAsync(usuarioId, 10);

        resumo.PercentualPlanoConcluido.Should().Be(50m);
        // Confirma que usou o plano mais recente, não o antigo.
        _planoItemRepository.Verify(r => r.GetByPlanoIdAsync(planoAntigo.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ObterResumoAsync_MapeiaRespostasHojeEAtividadesRecentes()
    {
        var usuarioId = Guid.NewGuid();
        ConfigurarPadraoSemDados(usuarioId);
        var questaoId = Guid.NewGuid();
        _respostaUsuarioRepository.Setup(r => r.ContarRespostasNoDiaAsync(usuarioId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _respostaUsuarioRepository.Setup(r => r.GetUltimasAsync(usuarioId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RespostaUsuario { Id = Guid.NewGuid(), UsuarioId = usuarioId, QuestaoId = questaoId, Correta = true, CriadoEm = DateTimeOffset.UtcNow }]);

        var resumo = await _service.ObterResumoAsync(usuarioId, 5);

        resumo.RespostasHoje.Should().Be(3);
        resumo.AtividadesRecentes.Should().ContainSingle(a => a.QuestaoId == questaoId && a.Correta);
    }
}
