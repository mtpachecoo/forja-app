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
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das pontuações da semana, da maior para a menor.</returns>
    Task<IReadOnlyList<Pontuacao>> GetRankingSemanalAsync(DateOnly semanaReferencia, CancellationToken cancellationToken = default);
}
