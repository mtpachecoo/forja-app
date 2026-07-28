using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace Forja.Api.Auth;

/// <summary>
/// Busca e interpreta um documento JWKS "puro" (sem um discovery document OpenID Connect por trás,
/// como é o caso do Neon Auth). Usado pelo <see cref="ConfigurationManager{T}"/> para manter o cache
/// das chaves públicas atualizado automaticamente.
/// </summary>
public class JwksOnlyConfigurationRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    /// <inheritdoc />
    public async Task<JsonWebKeySet> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel);
        return JsonWebKeySet.Create(json);
    }
}
