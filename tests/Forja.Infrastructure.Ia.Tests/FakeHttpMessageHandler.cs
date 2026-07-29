namespace Forja.Infrastructure.Ia.Tests;

/// <summary>
/// <see cref="HttpMessageHandler"/> falso que captura a última requisição enviada (URL, headers,
/// corpo) e devolve uma resposta pré-configurada, sem nenhuma chamada de rede real.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _resposta;

    public FakeHttpMessageHandler(HttpResponseMessage resposta)
    {
        _resposta = resposta;
    }

    public HttpRequestMessage? UltimaRequisicao { get; private set; }

    public string? CorpoDaUltimaRequisicao { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UltimaRequisicao = request;
        CorpoDaUltimaRequisicao = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return _resposta;
    }
}
