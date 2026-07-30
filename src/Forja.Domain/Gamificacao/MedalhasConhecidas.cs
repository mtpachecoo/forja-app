namespace Forja.Domain.Gamificacao;

/// <summary>
/// Identificadores fixos de medalhas semeadas por migration (dado de referência, não gerado em
/// runtime) — compartilhados entre o seed (<c>CriaContribuicaoReputacao</c>, em Forja.Infrastructure)
/// e o código que concede a medalha (<c>ContribuicaoService</c>, em Forja.Application).
/// </summary>
public static class MedalhasConhecidas
{
    /// <summary>Medalha concedida na primeira contribuição de conteúdo aprovada do usuário.</summary>
    public static readonly Guid PrimeiraContribuicaoAprovadaId = new("9f9c9b0e-0f3d-4a3b-9b0e-0f3d4a3b9b0e");
}
