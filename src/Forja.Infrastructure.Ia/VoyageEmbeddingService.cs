using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Forja.Application.Duvidas;
using Microsoft.Extensions.Configuration;

namespace Forja.Infrastructure.Ia;

/// <summary>
/// Implementação de <see cref="IGeradorDeEmbeddings"/> que chama a API REST da Voyage AI.
/// Não existe conector oficial da Voyage para o Semantic Kernel — por isso essa chamada é feita
/// diretamente via <see cref="HttpClient"/>, em vez de passar pelo Kernel.
/// </summary>
public class VoyageEmbeddingService : IGeradorDeEmbeddings
{
    private readonly HttpClient _httpClient;
    private readonly string _modelo;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    /// <param name="httpClient">
    /// Cliente HTTP configurado (base address e header de autenticação) via
    /// <see cref="DependencyInjection.AddIa"/>.
    /// </param>
    /// <param name="configuration">Configuração da aplicação, contendo <c>Voyage:Modelo</c>.</param>
    public VoyageEmbeddingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _modelo = configuration["Voyage:Modelo"]
            ?? throw new InvalidOperationException("Configuração 'Voyage:Modelo' não encontrada.");
    }

    /// <inheritdoc />
    public async Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken = default)
    {
        // input_type "query": a Voyage antepõe um prompt otimizado pra busca/recuperação, diferente
        // do prompt usado para embedar os documentos originais (chunks) na ingestão.
        var requisicao = new VoyageEmbeddingRequest([texto], _modelo, "query");

        using var resposta = await _httpClient.PostAsJsonAsync("embeddings", requisicao, cancellationToken);
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<VoyageEmbeddingResponse>(cancellationToken);
        if (corpo is null || corpo.Data.Count == 0)
        {
            throw new InvalidOperationException("Resposta vazia da API de embeddings da Voyage.");
        }

        return corpo.Data[0].Embedding;
    }

    private sealed record VoyageEmbeddingRequest(
        [property: JsonPropertyName("input")] string[] Input,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input_type")] string InputType);

    private sealed record VoyageEmbeddingResponse(
        [property: JsonPropertyName("data")] List<VoyageEmbeddingData> Data);

    private sealed record VoyageEmbeddingData(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
