using Forja.Application.Estudo;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class RegistrarRespostaComEfeitosServiceTests
{
    private readonly Mock<IRespostaService> _respostaService = new();
    private readonly Mock<IRevisaoEspacadaService> _revisaoEspacadaService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RegistrarRespostaComEfeitosService _service;

    public RegistrarRespostaComEfeitosServiceTests()
    {
        _service = new RegistrarRespostaComEfeitosService(_respostaService.Object, _revisaoEspacadaService.Object, _unitOfWork.Object);
    }

    private static RegistrarRespostaResultado CriarResultado(Guid usuarioId, Guid questaoId, bool correta)
    {
        var resposta = new RespostaUsuario { Id = Guid.NewGuid(), UsuarioId = usuarioId, QuestaoId = questaoId, Correta = correta };
        var questao = new Questao { Id = questaoId, Gabarito = "A" };
        var pontuacao = new Pontuacao { UsuarioId = usuarioId };
        return new RegistrarRespostaResultado(resposta, questao, pontuacao);
    }

    [TestMethod]
    public async Task RegistrarAsync_ChamaRespostaServiceEDepoisRevisaoEspacadaComACorrecaoObtida()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        var resultadoEsperado = CriarResultado(usuarioId, questaoId, correta: true);

        _respostaService
            .Setup(s => s.RegistrarRespostaAsync(usuarioId, questaoId, "A", 10_000, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        var resultado = await _service.RegistrarAsync(usuarioId, questaoId, "A", 10_000, null, false);

        resultado.Should().BeSameAs(resultadoEsperado);
        _revisaoEspacadaService.Verify(
            r => r.RegistrarRespostaAsync(usuarioId, questaoId, true, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistrarAsync_RespostaIncorreta_PassaCorretaFalsoParaRevisaoEspacada()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        var resultadoEsperado = CriarResultado(usuarioId, questaoId, correta: false);

        _respostaService
            .Setup(s => s.RegistrarRespostaAsync(usuarioId, questaoId, "B", 3_000, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);

        await _service.RegistrarAsync(usuarioId, questaoId, "B", 3_000, null, false);

        _revisaoEspacadaService.Verify(
            r => r.RegistrarRespostaAsync(usuarioId, questaoId, false, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistrarAsync_RespeitaAOrdem_RevisaoEspacadaSoEhChamadaDepoisDaResposta()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        var resultadoEsperado = CriarResultado(usuarioId, questaoId, correta: true);
        var ordem = new List<string>();

        _respostaService
            .Setup(s => s.RegistrarRespostaAsync(usuarioId, questaoId, "A", 1000, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado)
            .Callback(() => ordem.Add("resposta"));
        _revisaoEspacadaService
            .Setup(r => r.RegistrarRespostaAsync(usuarioId, questaoId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RevisaoEspacada())
            .Callback(() => ordem.Add("revisao"));
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .Callback(() => ordem.Add("savechanges"));

        await _service.RegistrarAsync(usuarioId, questaoId, "A", 1000, null, false);

        ordem.Should().Equal("resposta", "revisao", "savechanges");
    }

    [TestMethod]
    public async Task RegistrarAsync_QuandoRevisaoEspacadaFalha_NaoDeixaRespostaPersistidaSozinha()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();
        var resultadoEsperado = CriarResultado(usuarioId, questaoId, correta: true);

        _respostaService
            .Setup(s => s.RegistrarRespostaAsync(usuarioId, questaoId, "A", 10_000, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoEsperado);
        _revisaoEspacadaService
            .Setup(r => r.RegistrarRespostaAsync(usuarioId, questaoId, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha simulada na revisão espaçada"));

        var acao = () => _service.RegistrarAsync(usuarioId, questaoId, "A", 10_000, null, false);

        await acao.Should().ThrowAsync<InvalidOperationException>();

        // RespostaService já rodou (enfileirou resposta + pontuação nos repositórios), mas como a
        // revisão espaçada falhou antes do único SaveChangesAsync deste orquestrador, nada foi
        // efetivamente persistido — a resposta nunca fica salva sem a revisão espaçada atualizada.
        _respostaService.Verify(
            s => s.RegistrarRespostaAsync(usuarioId, questaoId, "A", 10_000, null, false, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
