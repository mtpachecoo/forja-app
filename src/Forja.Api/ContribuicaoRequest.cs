namespace Forja.Api;

/// <summary>
/// Corpo da requisição de <c>POST /contribuicoes</c>. O identificador do usuário nunca vem do
/// cliente — é resolvido a partir do token autenticado.
/// </summary>
/// <param name="TopicoId">Identificador do tópico ao qual a contribuição se refere.</param>
/// <param name="Tipo">Tipo do conteúdo, como texto (<c>"Video"</c> ou <c>"Pdf"</c>, sem diferenciar maiúsculas/minúsculas).</param>
/// <param name="Link">Link (URL absoluta) do conteúdo externo.</param>
public sealed record ContribuicaoRequest(Guid TopicoId, string Tipo, string Link);
