using System.Net.Http.Headers;
using Forja.Application.Duvidas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Forja.Infrastructure.Ia;

/// <summary>
/// Métodos de extensão para registrar os serviços de IA (RAG) no container de DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra o Semantic Kernel (com o conector de chat completion do Gemini) e o cliente de
    /// embeddings da Voyage AI. Trocar de provedor de LLM é mudança de configuração
    /// (<c>Gemini:Modelo</c>/<c>Gemini:ApiKey</c>) — trocar de conector (ex.: para outro provedor
    /// suportado pelo Semantic Kernel) é a única mudança de código necessária, isolada neste projeto.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação, contendo as chaves <c>Gemini</c> e <c>Voyage</c>.</param>
    /// <returns>A própria coleção de serviços, para encadeamento.</returns>
    public static IServiceCollection AddIa(this IServiceCollection services, IConfiguration configuration)
    {
        var geminiApiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Configuração 'Gemini:ApiKey' não encontrada.");
        var geminiModelo = configuration["Gemini:Modelo"]
            ?? throw new InvalidOperationException("Configuração 'Gemini:Modelo' não encontrada.");

        var kernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(geminiModelo, geminiApiKey)
            .Build();

        // Só o IChatCompletionService é registrado — nada em Forja.Application depende do Kernel em
        // si (plugins/function-calling não são usados nesta fase); registrá-lo à toa ficaria morto.
        services.AddSingleton(kernel.GetRequiredService<IChatCompletionService>());

        var voyageApiKey = configuration["Voyage:ApiKey"]
            ?? throw new InvalidOperationException("Configuração 'Voyage:ApiKey' não encontrada.");

        services.AddHttpClient<IGeradorDeEmbeddings, VoyageEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri("https://api.voyageai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", voyageApiKey);
        });

        return services;
    }
}
