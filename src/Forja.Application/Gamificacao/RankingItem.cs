namespace Forja.Application.Gamificacao;

/// <summary>
/// Uma posição no ranking semanal (RF-013).
/// </summary>
/// <param name="UsuarioId">Identificador do usuário.</param>
/// <param name="Nome">Nome do usuário.</param>
/// <param name="PontosSemanaAtual">Pontuação acumulada na semana atual.</param>
/// <param name="Posicao">Posição no ranking (1-based).</param>
public sealed record RankingItem(Guid UsuarioId, string Nome, int PontosSemanaAtual, int Posicao);
