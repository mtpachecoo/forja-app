using Forja.Domain.Usuarios;

namespace Forja.Api;

/// <summary>Resposta de <c>GET /usuarios/{id}/perfil</c>.</summary>
/// <param name="UsuarioId">Identificador do usuário.</param>
/// <param name="Nome">Nome do usuário.</param>
/// <param name="Badges">Nomes das medalhas conquistadas.</param>
/// <param name="ContribuicoesAprovadas">Quantidade de contribuições de conteúdo aprovadas.</param>
/// <param name="NivelReputacao">Nível de reputação por contribuição.</param>
public sealed record PerfilPublicoResponse(
    Guid UsuarioId, string Nome, IReadOnlyList<string> Badges, int ContribuicoesAprovadas, string NivelReputacao)
{
    /// <summary>Constrói a resposta a partir da projeção do read store.</summary>
    public static PerfilPublicoResponse De(PerfilPublico perfil) => new(
        perfil.UsuarioId,
        perfil.Nome,
        perfil.Badges,
        perfil.ContribuicoesAprovadas,
        perfil.NivelReputacao.ToString());
}
