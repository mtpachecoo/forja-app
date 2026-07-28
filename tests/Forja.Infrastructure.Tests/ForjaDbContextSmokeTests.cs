using Forja.Domain.Questoes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Infrastructure.Tests;

/// <summary>
/// Testes de leitura contra o banco Neon real, confirmando que o mapeamento do EF Core bate com o
/// dado já existente em produção. Requerem rede e o arquivo <c>.env</c> na raiz do repositório
/// (chave <c>ConnectionStrings__ForjaDb</c>). Não escrevem nada no banco.
/// </summary>
[TestClass]
public class ForjaDbContextSmokeTests
{
    private static ServiceProvider _serviceProvider = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        EnvFile.Load();

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _serviceProvider.Dispose();
    }

    [TestMethod]
    public async Task DeveLerUmaCarreiraExistente()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var carreira = await context.Carreiras.AsNoTracking().FirstOrDefaultAsync();

        carreira.Should().NotBeNull();
        carreira!.Nome.Should().NotBeNullOrWhiteSpace();
        TestContext.WriteLine($"Carreira lida: Id={carreira.Id}, Nome=\"{carreira.Nome}\"");
    }

    [TestMethod]
    public async Task DeveLerUmaQuestaoDeMultiplaEscolhaComAlternativas()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var questao = await context.Questoes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Tipo == TipoQuestao.MultiplaEscolha);

        questao.Should().NotBeNull();
        questao!.Alternativas.Should().NotBeNull();
        questao.Alternativas.Should().NotBeEmpty();
        TestContext.WriteLine($"Questao lida: Id={questao.Id}, {questao.Alternativas!.Count} alternativas (letras: {string.Join(",", questao.Alternativas.Select(a => a.Letra))})");
    }

    [TestMethod]
    public async Task DeveLerUmChunkDeConteudoComEmbeddingDe1024Posicoes()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var chunk = await context.ChunksConteudo.AsNoTracking().FirstOrDefaultAsync();

        chunk.Should().NotBeNull();
        chunk!.Embedding.Should().HaveCount(1024);
        TestContext.WriteLine($"Chunk lido: Id={chunk.Id}, Embedding.Length={chunk.Embedding.Length}");
    }
}
