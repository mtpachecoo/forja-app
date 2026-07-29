using Forja.Application.Gamificacao;

namespace Forja.Api;

/// <summary>Uma posição no ranking semanal (<c>GET /ranking/semanal</c>).</summary>
/// <param name="Posicao">Posição no ranking (1-based).</param>
/// <param name="UsuarioId">Identificador do usuário.</param>
/// <param name="Nome">Nome do usuário.</param>
/// <param name="PontosSemanaAtual">Pontuação acumulada na semana atual.</param>
public sealed record RankingResponseItem(int Posicao, Guid UsuarioId, string Nome, int PontosSemanaAtual)
{
    /// <summary>Constrói o item a partir do resultado do serviço.</summary>
    public static RankingResponseItem De(RankingItem item) => new(
        item.Posicao,
        item.UsuarioId,
        item.Nome,
        item.PontosSemanaAtual);
}
