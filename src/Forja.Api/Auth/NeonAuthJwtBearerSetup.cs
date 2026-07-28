using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using ScottBrady.IdentityModel.Crypto;
using ScottBrady.IdentityModel.Tokens;

namespace Forja.Api.Auth;

/// <summary>
/// Configura a validação de tokens JWT emitidos pelo Neon Auth ("Managed Better Auth").
/// </summary>
public static class NeonAuthJwtBearerSetup
{
    /// <summary>
    /// Registra o esquema de autenticação JWT Bearer configurado para validar tokens do Neon Auth.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação, contendo <c>NeonAuth:BaseUrl</c>.</param>
    /// <returns>A própria coleção de serviços, para encadeamento.</returns>
    public static IServiceCollection AddNeonAuthJwtBearer(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["NeonAuth:BaseUrl"]
            ?? throw new InvalidOperationException("Configuração 'NeonAuth:BaseUrl' não encontrada.");
        var issuer = new Uri(baseUrl).GetLeftPart(UriPartial.Authority);
        var jwksUrl = $"{baseUrl.TrimEnd('/')}/.well-known/jwks.json";

        var jwksConfigManager = new ConfigurationManager<JsonWebKeySet>(
            jwksUrl,
            new JwksOnlyConfigurationRetriever(),
            new HttpDocumentRetriever());

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Mantém a claim "sub" crua no ClaimsPrincipal, sem o remapeamento padrão do
                // JwtSecurityTokenHandler para ClaimTypes.NameIdentifier (URI longa).
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (_, _, kid, _) =>
                        ResolverChavesEdDsa(jwksConfigManager, kid),
                };
            });

        return services;
    }

    private static IEnumerable<SecurityKey> ResolverChavesEdDsa(ConfigurationManager<JsonWebKeySet> jwksConfigManager, string? kid)
    {
        var jwks = jwksConfigManager.GetConfigurationAsync().GetAwaiter().GetResult();

        var chaves = string.IsNullOrEmpty(kid)
            ? jwks.Keys
            : jwks.Keys.Where(k => k.Kid == kid).ToList();

        return chaves
            .Where(k => k.Kty == "OKP" && k.Crv == "Ed25519")
            .Select(k => (SecurityKey)new EdDsaSecurityKey(EdDsa.Create(
                new EdDsaParameters(ExtendedSecurityAlgorithms.Curves.Ed25519)
                {
                    X = Base64UrlEncoder.DecodeBytes(k.X),
                })));
    }
}
