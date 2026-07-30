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
    /// <param name="skip">Quantidade de posições a pular (paginação). Padrão 0.</param>
    /// <param name="take">Quantidade máxima de posições a retornar (paginação). Padrão 50, com teto interno.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura do ranking, da maior para a menor pontuação, já paginada.</returns>
    Task<IReadOnlyList<RankingItem>> ObterRankingSemanalAsync(
        Guid? carreiraId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}
