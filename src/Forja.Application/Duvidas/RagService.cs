using Forja.Application.Common;
using Forja.Application.Questoes;
using Forja.Domain.Conteudo;

namespace Forja.Application.Duvidas;

/// <summary>
/// Implementação padrão de <see cref="IRagService"/>.
/// </summary>
public class RagService : IRagService
{
    /// <summary>Quantidade de chunks recuperados por busca de similaridade.</summary>
    private const int QuantidadeDeChunks = 5;

    private const string InstrucaoDeSistema =
        "Você é um assistente de estudos. Responda à dúvida do aluno se baseando ESTRITAMENTE no " +
        "contexto fornecido abaixo, extraído do material de estudo. Não use conhecimento próprio " +
        "além do que está no contexto. Se o contexto não tiver informação suficiente para responder, " +
        "diga isso explicitamente em vez de complementar com conhecimento externo.";

    private readonly IQuestaoService _questaoService;
    private readonly IChunkConteudoRepository _chunkRepository;
    private readonly IGeradorDeEmbeddings _geradorDeEmbeddings;
    private readonly IGeradorDeRespostaChat _geradorDeRespostaChat;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    /// <param name="questaoService">Serviço de questões, para validar a questão e obter seu enunciado.</param>
    /// <param name="chunkRepository">Repositório de chunks de conteúdo, para a busca por similaridade.</param>
    /// <param name="geradorDeEmbeddings">Gerador de embeddings, para embedar a pergunta do aluno.</param>
    /// <param name="geradorDeRespostaChat">Gerador de resposta de chat via LLM.</param>
    public RagService(
        IQuestaoService questaoService,
        IChunkConteudoRepository chunkRepository,
        IGeradorDeEmbeddings geradorDeEmbeddings,
        IGeradorDeRespostaChat geradorDeRespostaChat)
    {
        _questaoService = questaoService;
        _chunkRepository = chunkRepository;
        _geradorDeEmbeddings = geradorDeEmbeddings;
        _geradorDeRespostaChat = geradorDeRespostaChat;
    }

    /// <inheritdoc />
    public async Task<RespostaDuvida> ResponderDuvidaAsync(Guid questaoId, string pergunta, CancellationToken cancellationToken = default)
    {
        var questao = await _questaoService.ObterPorIdAsync(questaoId, cancellationToken);

        var embeddingPergunta = await _geradorDeEmbeddings.GerarEmbeddingAsync(pergunta, cancellationToken);
        var chunks = await _chunkRepository.BuscarPorSimilaridadeAsync(embeddingPergunta, QuantidadeDeChunks, cancellationToken);

        if (chunks.Count == 0)
        {
            return new RespostaDuvida(
                "Não encontrei conteúdo suficiente na base de estudo para responder essa dúvida com segurança.",
                []);
        }

        var respostaLlm = await _geradorDeRespostaChat.GerarRespostaAsync(
            MontarPrompt(questao.Enunciado, pergunta, chunks), InstrucaoDeSistema, cancellationToken);

        return new RespostaDuvida(respostaLlm ?? string.Empty, chunks.Select(c => c.Id).ToList());
    }

    private static string MontarPrompt(string enunciadoQuestao, string pergunta, IReadOnlyList<ChunkConteudo> chunks)
    {
        var contexto = string.Join(
            "\n\n---\n\n",
            chunks.Select((chunk, indice) => $"[Trecho {indice + 1}]\n{chunk.Texto}"));

        return $"""
            Questão: {enunciadoQuestao}

            Contexto recuperado da base de conteúdo:
            {contexto}

            Dúvida do aluno: {pergunta}
            """;
    }
}
