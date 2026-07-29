namespace Forja.Application.Gamificacao;

/// <summary>
/// Serviço de pontuação e ranking (RF-013).
/// </summary>
public interface IPontuacaoService
{
    /// <summary>
    /// Obtém o ranking semanal, ordenado por pontuação da semana atual (decrescente), opcionalmente
    /// restrito aos usuários que estudam para uma carreira específica.
    /// </summary>
    /// <param name="carreiraId">
    /// Identificador da carreira para restringir o ranking, ou <c>null</c> para o ranking geral. A
    /// restrição usa os usuários com plano de estudo para essa carreira — <see cref="Domain.Gamificacao.Pontuacao"/>
    /// é global por usuário, não segmentada por carreira.
    /// </param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura do ranking, da maior para a menor pontuação.</returns>
    Task<IReadOnlyList<RankingItem>> ObterRankingSemanalAsync(Guid? carreiraId, CancellationToken cancellationToken = default);
}
