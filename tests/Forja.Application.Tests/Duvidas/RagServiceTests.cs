using Forja.Application.Duvidas;
using Forja.Application.Questoes;
using Forja.Domain.Conteudo;
using Forja.Domain.Questoes;
using FluentAssertions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace Forja.Application.Tests.Duvidas;

[TestClass]
public class RagServiceTests
{
    private readonly Mock<IQuestaoService> _questaoService = new();
    private readonly Mock<IChunkConteudoRepository> _chunkRepository = new();
    private readonly Mock<IGeradorDeEmbeddings> _geradorDeEmbeddings = new();
    private readonly Mock<IChatCompletionService> _chatCompletionService = new();
    private readonly RagService _service;

    public RagServiceTests()
    {
        _service = new RagService(
            _questaoService.Object,
            _chunkRepository.Object,
            _geradorDeEmbeddings.Object,
            _chatCompletionService.Object);
    }

    private void ConfigurarRespostaDoLlm(string texto)
    {
        _chatCompletionService
            .Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, texto)]);
    }

    [TestMethod]
    public async Task ResponderDuvidaAsync_MontaPromptComTextoDosChunksRecuperados()
    {
        var questaoId = Guid.NewGuid();
        var questao = new Questao { Id = questaoId, Enunciado = "Qual o prazo prescricional?", Status = StatusQuestao.Aprovada };
        var embeddingPergunta = new float[] { 0.1f, 0.2f, 0.3f };
        var chunks = new List<ChunkConteudo>
        {
            new() { Id = Guid.NewGuid(), Texto = "Trecho sobre prescricao quinquenal do Decreto 20.910/32." },
            new() { Id = Guid.NewGuid(), Texto = "Trecho sobre interrupcao do prazo prescricional." },
        };

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>())).ReturnsAsync(questao);
        _geradorDeEmbeddings.Setup(g => g.GerarEmbeddingAsync("o que significa isso?", It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingPergunta);
        _chunkRepository.Setup(r => r.BuscarPorSimilaridadeAsync(embeddingPergunta, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunks);

        ChatHistory? historicoCapturado = null;
        _chatCompletionService
            .Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Callback<ChatHistory, PromptExecutionSettings, Kernel, CancellationToken>(
                (historico, _, _, _) => historicoCapturado = historico)
            .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, "resposta grounded")]);

        var resultado = await _service.ResponderDuvidaAsync(questaoId, "o que significa isso?");

        historicoCapturado.Should().NotBeNull();
        var promptDoUsuario = string.Concat(historicoCapturado!.Where(m => m.Role == AuthorRole.User).Select(m => m.Content));
        promptDoUsuario.Should().Contain(chunks[0].Texto);
        promptDoUsuario.Should().Contain(chunks[1].Texto);
        promptDoUsuario.Should().Contain(questao.Enunciado);

        resultado.Resposta.Should().Be("resposta grounded");
        resultado.ChunksUsadosIds.Should().BeEquivalentTo(chunks.Select(c => c.Id));
    }

    [TestMethod]
    public async Task ResponderDuvidaAsync_NenhumChunkRecuperado_NaoChamaOLlmERetornaMensagemPadrao()
    {
        var questaoId = Guid.NewGuid();
        var questao = new Questao { Id = questaoId, Enunciado = "Enunciado qualquer", Status = StatusQuestao.Aprovada };

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>())).ReturnsAsync(questao);
        _geradorDeEmbeddings.Setup(g => g.GerarEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });
        _chunkRepository.Setup(r => r.BuscarPorSimilaridadeAsync(It.IsAny<float[]>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await _service.ResponderDuvidaAsync(questaoId, "pergunta sem contexto disponivel");

        resultado.ChunksUsadosIds.Should().BeEmpty();
        resultado.Resposta.Should().NotBeNullOrEmpty();
        _chatCompletionService.Verify(
            c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ResponderDuvidaAsync_UsaOEmbeddingDaPerguntaParaBuscarOsChunks()
    {
        var questaoId = Guid.NewGuid();
        var questao = new Questao { Id = questaoId, Enunciado = "Enunciado", Status = StatusQuestao.Aprovada };
        var embeddingPergunta = new float[] { 0.5f, 0.6f };

        _questaoService.Setup(s => s.ObterPorIdAsync(questaoId, It.IsAny<CancellationToken>())).ReturnsAsync(questao);
        _geradorDeEmbeddings.Setup(g => g.GerarEmbeddingAsync("pergunta X", It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingPergunta);
        _chunkRepository.Setup(r => r.BuscarPorSimilaridadeAsync(embeddingPergunta, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ChunkConteudo { Id = Guid.NewGuid(), Texto = "texto" }]);
        ConfigurarRespostaDoLlm("ok");

        await _service.ResponderDuvidaAsync(questaoId, "pergunta X");

        _chunkRepository.Verify(r => r.BuscarPorSimilaridadeAsync(embeddingPergunta, 5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
