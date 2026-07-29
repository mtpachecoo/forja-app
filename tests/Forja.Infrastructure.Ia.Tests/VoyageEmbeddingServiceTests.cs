using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Forja.Infrastructure.Ia.Tests;

[TestClass]
public class VoyageEmbeddingServiceTests
{
    private static IConfiguration ConfigurarComModelo(string modelo = "voyage-3")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Voyage:Modelo"] = modelo })
            .Build();
    }

    private static (VoyageEmbeddingService Servico, FakeHttpMessageHandler Handler) CriarServico(HttpResponseMessage resposta, string modelo = "voyage-3")
    {
        var handler = new FakeHttpMessageHandler(resposta);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.voyageai.com/v1/") };
        var servico = new VoyageEmbeddingService(httpClient, ConfigurarComModelo(modelo));
        return (servico, handler);
    }

    private static HttpResponseMessage RespostaComEmbedding(params float[] embedding)
    {
        var corpo = JsonSerializer.Serialize(new { data = new[] { new { embedding } } });
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(corpo) };
    }

    [TestMethod]
    public async Task GerarEmbeddingAsync_MontaARequisicaoComUrlModeloEInputTypeCorretos()
    {
        var (servico, handler) = CriarServico(RespostaComEmbedding(0.1f, 0.2f), modelo: "voyage-3");

        await servico.GerarEmbeddingAsync("qual o prazo prescricional?");

        handler.UltimaRequisicao.Should().NotBeNull();
        handler.UltimaRequisicao!.Method.Should().Be(HttpMethod.Post);
        handler.UltimaRequisicao.RequestUri.Should().Be(new Uri("https://api.voyageai.com/v1/embeddings"));

        handler.CorpoDaUltimaRequisicao.Should().NotBeNull();
        using var corpo = JsonDocument.Parse(handler.CorpoDaUltimaRequisicao!);
        corpo.RootElement.GetProperty("model").GetString().Should().Be("voyage-3");
        corpo.RootElement.GetProperty("input_type").GetString().Should().Be("query");
        corpo.RootElement.GetProperty("input")[0].GetString().Should().Be("qual o prazo prescricional?");
    }

    [TestMethod]
    public async Task GerarEmbeddingAsync_UsaOHeaderDeAutenticacaoJaConfiguradoNoHttpClient()
    {
        var handler = new FakeHttpMessageHandler(RespostaComEmbedding(0.1f));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.voyageai.com/v1/") };
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "chave-de-teste");
        var servico = new VoyageEmbeddingService(httpClient, ConfigurarComModelo());

        await servico.GerarEmbeddingAsync("pergunta");

        handler.UltimaRequisicao!.Headers.Authorization.Should().NotBeNull();
        handler.UltimaRequisicao.Headers.Authorization!.ToString().Should().Be("Bearer chave-de-teste");
    }

    [TestMethod]
    public async Task GerarEmbeddingAsync_RespostaValida_RetornaOEmbeddingParseado()
    {
        var (servico, _) = CriarServico(RespostaComEmbedding(0.1f, 0.2f, 0.3f));

        var embedding = await servico.GerarEmbeddingAsync("pergunta");

        embedding.Should().BeEquivalentTo([0.1f, 0.2f, 0.3f], options => options.WithStrictOrdering());
    }

    [TestMethod]
    public async Task GerarEmbeddingAsync_RespostaComErroHttp_LancaHttpRequestException()
    {
        var (servico, _) = CriarServico(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        var acao = () => servico.GerarEmbeddingAsync("pergunta");

        await acao.Should().ThrowAsync<HttpRequestException>();
    }

    [TestMethod]
    public async Task GerarEmbeddingAsync_RespostaSemDados_LancaInvalidOperationException()
    {
        var respostaVazia = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"data": []}"""),
        };
        var (servico, _) = CriarServico(respostaVazia);

        var acao = () => servico.GerarEmbeddingAsync("pergunta");

        await acao.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task GerarEmbeddingAsync_RespostaMalformada_LancaExcecaoDeDesserializacao()
    {
        var respostaMalformada = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("isso nao e json valido"),
        };
        var (servico, _) = CriarServico(respostaMalformada);

        var acao = () => servico.GerarEmbeddingAsync("pergunta");

        await acao.Should().ThrowAsync<Exception>();
    }
}
