using Forja.Application.Common;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Forja.Infrastructure.Ia;

/// <summary>
/// Implementação de <see cref="IGeradorDeRespostaChat"/> via Semantic Kernel. Único ponto do código
/// que ainda conhece <see cref="IChatCompletionService"/>/<see cref="ChatHistory"/> — o resto de
/// Forja.Application depende só da porta.
/// </summary>
public class SemanticKernelChatService : IGeradorDeRespostaChat
{
    private readonly IChatCompletionService _chatCompletionService;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    /// <param name="chatCompletionService">Serviço de chat completion do Semantic Kernel, já configurado via <see cref="DependencyInjection.AddIa"/>.</param>
    public SemanticKernelChatService(IChatCompletionService chatCompletionService)
    {
        _chatCompletionService = chatCompletionService;
    }

    /// <inheritdoc />
    public async Task<string?> GerarRespostaAsync(string prompt, string? instrucaoDeSistema = null, CancellationToken cancellationToken = default)
    {
        var historico = new ChatHistory();
        if (instrucaoDeSistema is not null)
        {
            historico.AddSystemMessage(instrucaoDeSistema);
        }

        historico.AddUserMessage(prompt);

        var respostas = await _chatCompletionService.GetChatMessageContentsAsync(historico, cancellationToken: cancellationToken);
        return respostas[0].Content;
    }
}
