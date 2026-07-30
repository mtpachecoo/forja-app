using Forja.Domain.Conteudo;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Conteudo;

/// <summary>
/// Teste de integração de <see cref="Forja.Infrastructure.Repositories.ChunkConteudoRepository.BuscarPorSimilaridadeAsync"/>
/// contra um Postgres real com pgvector (Testcontainers) — o único método de todo o código que usa SQL
/// cru (<c>FromSqlInterpolated</c> com o operador <c>&lt;=&gt;</c> do pgvector) e que, antes deste teste,
/// não tinha nenhuma cobertura própria: <c>RagServiceTests</c> mocka <see cref="IChunkConteudoRepository"/>
/// inteiro, nunca exercitando a sintaxe SQL de fato contra o banco.
/// </summary>
[TestClass]
public class ChunkConteudoRepositoryIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private const int Dimensoes = 1024;

    private static float[] CriarEmbedding(params (int Indice, float Valor)[] componentes)
    {
        var vetor = new float[Dimensoes];
        foreach (var (indice, valor) in componentes)
        {
            vetor[indice] = valor;
        }

        return vetor;
    }

    [TestMethod]
    public async Task BuscarPorSimilaridadeAsync_OrdenaPorDistanciaDeCossenoERespeitaOLimite()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var fonte = new FonteConteudo
        {
            Id = Guid.NewGuid(),
            Tipo = TipoFonte.Lei,
            Titulo = "Fonte de teste",
            TextoExtraido = "texto",
            VersaoEm = DateTimeOffset.UtcNow,
            CriadoEm = DateTimeOffset.UtcNow,
        };
        context.FontesConteudo.Add(fonte);

        // Referência de busca: vetor unitário na dimensão 0.
        // chunkIdentico: mesma direção da referência -> distância de cosseno 0 (o mais similar).
        // chunkParcial: 45 graus da referência -> distância ~0,293 (similaridade intermediária).
        // chunkOrtogonal: perpendicular à referência -> distância 1 (o menos similar).
        var chunkIdentico = new ChunkConteudo { Id = Guid.NewGuid(), FonteId = fonte.Id, Texto = "identico", Embedding = CriarEmbedding((0, 1f)), CriadoEm = DateTimeOffset.UtcNow };
        var chunkParcial = new ChunkConteudo { Id = Guid.NewGuid(), FonteId = fonte.Id, Texto = "parcial", Embedding = CriarEmbedding((0, 0.5f), (1, 0.5f)), CriadoEm = DateTimeOffset.UtcNow };
        var chunkOrtogonal = new ChunkConteudo { Id = Guid.NewGuid(), FonteId = fonte.Id, Texto = "ortogonal", Embedding = CriarEmbedding((2, 1f)), CriadoEm = DateTimeOffset.UtcNow };
        context.ChunksConteudo.AddRange(chunkOrtogonal, chunkParcial, chunkIdentico); // ordem de insert embaralhada de propósito
        await context.SaveChangesAsync();

        var repositorio = escopo.ServiceProvider.GetRequiredService<IChunkConteudoRepository>();
        var vetorDeBusca = CriarEmbedding((0, 1f));

        var resultado = await repositorio.BuscarPorSimilaridadeAsync(vetorDeBusca, quantidade: 2);

        resultado.Should().HaveCount(2, "quantidade limita o resultado mesmo havendo 3 chunks candidatos");
        resultado[0].Id.Should().Be(chunkIdentico.Id, "é o chunk com menor distância de cosseno até a referência");
        resultado[1].Id.Should().Be(chunkParcial.Id, "é o segundo mais próximo, antes do ortogonal");
    }
}
