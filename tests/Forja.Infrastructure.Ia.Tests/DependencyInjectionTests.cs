using Forja.Application.Common;
using Forja.Application.Duvidas;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Forja.Infrastructure.Ia.Tests;

/// <summary>
/// Cobre só o registro de DI de <see cref="DependencyInjection.AddIa"/> — resolve o container real e
/// confirma que cada serviço resolve sem exceção. Não chama nenhuma API de IA de verdade.
/// </summary>
[TestClass]
public class DependencyInjectionTests
{
    private static ServiceProvider ConstruirProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "chave-de-teste",
                ["Gemini:Modelo"] = "gemini-2.5-flash",
                ["Voyage:ApiKey"] = "chave-de-teste",
                ["Voyage:Modelo"] = "voyage-3",
            })
            .Build();

        var services = new ServiceCollection();
        // Em produção, o host do ASP.NET Core registra IConfiguration automaticamente — aqui, com um
        // ServiceCollection puro, isso precisa ser feito manualmente pra VoyageEmbeddingService
        // (que injeta IConfiguration direto no construtor) resolver.
        services.AddSingleton<IConfiguration>(configuration);
        services.AddIa(configuration);
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public void AddIa_ResolveIChatCompletionServiceSemLancar()
    {
        using var provider = ConstruirProvider();

        var acao = () => provider.GetRequiredService<IChatCompletionService>();

        acao.Should().NotThrow();
    }

    [TestMethod]
    public void AddIa_ResolveIGeradorDeRespostaChatComoSemanticKernelChatService()
    {
        using var provider = ConstruirProvider();

        var acao = () => provider.GetRequiredService<IGeradorDeRespostaChat>();

        acao.Should().NotThrow();
        provider.GetRequiredService<IGeradorDeRespostaChat>().Should().BeOfType<SemanticKernelChatService>();
    }

    [TestMethod]
    public void AddIa_ResolveIGeradorDeEmbeddingsComoVoyageEmbeddingService()
    {
        using var provider = ConstruirProvider();

        var acao = () => provider.GetRequiredService<IGeradorDeEmbeddings>();

        acao.Should().NotThrow();
        provider.GetRequiredService<IGeradorDeEmbeddings>().Should().BeOfType<VoyageEmbeddingService>();
    }
}
