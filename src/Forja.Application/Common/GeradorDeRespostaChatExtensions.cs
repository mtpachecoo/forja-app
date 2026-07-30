namespace Forja.Application.Common;

/// <summary>
/// Métodos de extensão para <see cref="IGeradorDeRespostaChat"/> que combinam os passos repetidos por
/// todo serviço que pede uma resposta JSON estruturada a um LLM: montar o prompt, chamar o modelo e
/// desserializar a resposta via <see cref="IaJsonResponseParser"/>.
/// </summary>
public static class GeradorDeRespostaChatExtensions
{
    /// <summary>
    /// Pede ao LLM uma resposta e tenta desserializá-la como <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Tipo esperado da resposta estruturada.</typeparam>
    /// <param name="gerador">Gerador de resposta de chat.</param>
    /// <param name="prompt">Prompt/pergunta do usuário, já instruindo o formato JSON esperado.</param>
    /// <param name="instrucaoDeSistema">
    /// Instrução de sistema que rege o comportamento do modelo, ou <c>null</c> quando o prompt já é autossuficiente.
    /// </param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>
    /// O objeto desserializado, ou <c>null</c> se o modelo não retornar conteúdo ou a resposta não for
    /// um JSON válido para <typeparamref name="T"/>.
    /// </returns>
    public static async Task<T?> PedirRespostaEstruturadaAsync<T>(
        this IGeradorDeRespostaChat gerador,
        string prompt,
        string? instrucaoDeSistema = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var resposta = await gerador.GerarRespostaAsync(prompt, instrucaoDeSistema, cancellationToken);
        if (string.IsNullOrWhiteSpace(resposta))
        {
            return null;
        }

        return IaJsonResponseParser.TentarDesserializar<T>(resposta);
    }
}
