using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Application.Questoes;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class RespostaServiceTests
{
    private readonly Mock<IQuestaoService> _questaoService = new();
    private readonly Mock<IRespostaUsuarioRepository> _respostaRepository = new();
    private readonly Mock<IPontuacaoRepository> _pontuacaoRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RespostaService _service;

    public RespostaServiceTests()
    {
        _service = new RespostaService(
            _questaoService.Object,
            _respostaRepository.Object,
            _pontuacaoRepository.Object,
            _unitOfWork.Object);
    }

    private static Questao CriarQuestaoAprovada(Guid id, string gabarito = "A") => new()
    {
        Id = id,
        Gabarito = gabarito,
        Status = StatusQuestao.Aprovada,
    };

    [TestMethod]
    public async Task RegistrarRespostaAsync_RespostaCorretaPrimeiraVez_Pontua()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarQuestaoAprovada(questaoId));
        _respostaRepository.Setup(r => r.ExisteRespostaPontuadaAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _pontuacaoRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pontuacao?)null);

        var resultado = await _service.RegistrarRespostaAsync(usuarioId, questaoId, "A", tempoRespostaMs: 10_000, pomodoroId: null, ehRevisao: false);

        resultado.Resposta.Correta.Should().BeTrue();
        resultado.Resposta.Pontuada.Should().BeTrue();
        resultado.Resposta.PontosConcedidos.Should().Be(10);
        resultado.Pontuacao.PontosTotal.Should().Be(10);
        resultado.Pontuacao.PontosSemanaAtual.Should().Be(10);

        _pontuacaoRepository.Verify(r => r.AddAsync(It.Is<Pontuacao>(p => p.UsuarioId == usuarioId && p.PontosTotal == 10), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_QuestaoJaPontuadaAnteriormente_NaoPontuaDeNovo()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarQuestaoAprovada(questaoId));
        _respostaRepository.Setup(r => r.ExisteRespostaPontuadaAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _pontuacaoRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pontuacao { UsuarioId = usuarioId, PontosTotal = 10, PontosSemanaAtual = 10, SemanaReferencia = DateOnly.FromDateTime(DateTime.UtcNow) });

        var resultado = await _service.RegistrarRespostaAsync(usuarioId, questaoId, "A", tempoRespostaMs: 10_000, pomodoroId: null, ehRevisao: false);

        resultado.Resposta.Correta.Should().BeTrue();
        resultado.Resposta.Pontuada.Should().BeFalse();
        resultado.Resposta.PontosConcedidos.Should().Be(0);

        _pontuacaoRepository.Verify(r => r.AddAsync(It.IsAny<Pontuacao>(), It.IsAny<CancellationToken>()), Times.Never);
        _pontuacaoRepository.Verify(r => r.Update(It.IsAny<Pontuacao>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_RespostaAbaixoDoTempoMinimo_EhChuteENaoPontua()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarQuestaoAprovada(questaoId));
        _respostaRepository.Setup(r => r.ExisteRespostaPontuadaAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _pontuacaoRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pontuacao?)null);

        // Resposta correta, mas dada em menos de 5s (tempo mínimo) — deve ser tratada como chute.
        var resultado = await _service.RegistrarRespostaAsync(usuarioId, questaoId, "A", tempoRespostaMs: 2_000, pomodoroId: null, ehRevisao: false);

        resultado.Resposta.Correta.Should().BeTrue();
        resultado.Resposta.Pontuada.Should().BeFalse();
        resultado.Resposta.PontosConcedidos.Should().Be(0);

        _pontuacaoRepository.Verify(r => r.AddAsync(It.IsAny<Pontuacao>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_QuandoFalhaAoProcessarPontuacao_NaoDeixaRespostaPersistidaSozinha()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarQuestaoAprovada(questaoId));
        _respostaRepository.Setup(r => r.ExisteRespostaPontuadaAsync(usuarioId, questaoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _pontuacaoRepository.Setup(r => r.GetByIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha simulada ao ler a pontuação"));

        var acao = () => _service.RegistrarRespostaAsync(usuarioId, questaoId, "A", tempoRespostaMs: 10_000, pomodoroId: null, ehRevisao: false);

        await acao.Should().ThrowAsync<InvalidOperationException>();

        // A resposta foi enfileirada no change tracker (AddAsync), mas como a exceção interrompeu o
        // fluxo antes do único SaveChangesAsync, nada foi efetivamente persistido — resposta e
        // pontuação só existem juntas no banco, nunca uma sem a outra.
        _respostaRepository.Verify(r => r.AddAsync(It.IsAny<RespostaUsuario>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task RegistrarRespostaAsync_QuestaoInexistente_LancaQuestaoNaoEncontrada()
    {
        var usuarioId = Guid.NewGuid();
        var questaoId = Guid.NewGuid();

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Questão", questaoId));

        var acao = () => _service.RegistrarRespostaAsync(usuarioId, questaoId, "A", tempoRespostaMs: 10_000, pomodoroId: null, ehRevisao: false);

        await acao.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
