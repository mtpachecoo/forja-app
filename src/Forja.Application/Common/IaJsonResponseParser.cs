using System.Text.Json;

namespace Forja.Application.Common;

/// <summary>
/// Desserializa respostas de LLM que deveriam ser JSON estrito, mas podem vir envolvidas em cercas de
/// markdown (```` ```json ... ``` ````) apesar da instrução do prompt. Compartilhado por todo serviço
/// que pede uma resposta estruturada a um <see cref="Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService"/>
/// (ex.: <see cref="Forja.Application.Estudo.PesoDisciplinaService"/>, <see cref="Forja.Application.Estudo.PlanoEstudoService"/>),
/// evitando duplicar a mesma limpeza + desserialização em cada um.
/// </summary>
public static class IaJsonResponseParser
{
    /// <summary>
    /// Tenta desserializar o texto de resposta do LLM como <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Tipo alvo da desserialização.</typeparam>
    /// <param name="texto">Texto bruto retornado pelo LLM.</param>
    /// <returns>O objeto desserializado, ou <c>null</c> se o texto não for um JSON válido para o tipo.</returns>
    public static T? TentarDesserializar<T>(string texto) where T : class
    {
        var limpo = texto.Trim();
        if (limpo.StartsWith("```"))
        {
            var primeiraQuebra = limpo.IndexOf('\n');
            var fimBloco = limpo.LastIndexOf("```", StringComparison.Ordinal);
            if (primeiraQuebra >= 0 && fimBloco > primeiraQuebra)
            {
                limpo = limpo[(primeiraQuebra + 1)..fimBloco].Trim();
            }
        }

        try
        {
            return JsonSerializer.Deserialize<T>(limpo, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
