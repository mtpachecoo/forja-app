using Forja.Domain.Common;

namespace Forja.Domain.Gamificacao;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Pontuacao"/>.
/// </summary>
public interface IPontuacaoRepository : IRepository<Pontuacao, Guid>
{
    /// <summary>
    /// Obtém as pontuações da semana informada, ordenadas por <see cref="Pontuacao.PontosSemanaAtual"/>
    /// decrescente — usado para montar o ranking semanal (RF-013).
    /// </summary>
    /// <param name="semanaReferencia">Data de início da semana de referência.</param>
    /// <param name="usuarioIds">
    /// Quando informado, restringe o ranking a esses usuários (ex.: usuários de uma carreira específica).
    /// A restrição é aplicada antes da paginação, para que <paramref name="skip"/>/<paramref name="take"/>
    /// naveguem corretamente pelo conjunto já filtrado. <c>null</c> para o ranking geral.
    /// </param>
    /// <param name="skip">Quantidade de posições a pular (paginação).</param>
    /// <param name="take">Quantidade máxima de posições a retornar (paginação).</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das pontuações da semana, da maior para a menor, já paginada.</returns>
    Task<IReadOnlyList<Pontuacao>> GetRankingSemanalAsync(
        DateOnly semanaReferencia,
        IReadOnlyCollection<Guid>? usuarioIds,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
