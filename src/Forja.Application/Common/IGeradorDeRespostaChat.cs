namespace Forja.Application.Common;

/// <summary>
/// Porta para geração de respostas de chat via LLM. Mesmo papel de <see cref="Forja.Application.Duvidas.IGeradorDeEmbeddings"/>
/// para embeddings: isola Forja.Application de qualquer SDK específico de IA (ex.: Semantic Kernel) —
/// a implementação concreta e o conector do provedor de LLM ficam inteiramente em Forja.Infrastructure.Ia.
/// Trocar de provedor/conector é mudança de infraestrutura, não de domínio.
/// </summary>
public interface IGeradorDeRespostaChat
{
    /// <summary>
    /// Gera uma resposta de chat a partir de um prompt, com uma instrução de sistema opcional.
    /// </summary>
    /// <param name="prompt">Prompt/pergunta do usuário.</param>
    /// <param name="instrucaoDeSistema">
    /// Instrução de sistema que rege o comportamento do modelo, ou <c>null</c> quando o prompt já é autossuficiente.
    /// </param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O texto da resposta gerada, ou <c>null</c>/vazio se o modelo não retornar conteúdo.</returns>
    Task<string?> GerarRespostaAsync(string prompt, string? instrucaoDeSistema = null, CancellationToken cancellationToken = default);
}
