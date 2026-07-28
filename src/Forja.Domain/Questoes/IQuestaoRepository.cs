using Forja.Domain.Common;

namespace Forja.Domain.Questoes;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Questao"/>.
/// </summary>
public interface IQuestaoRepository : IRepository<Questao, Guid>
{
    /// <summary>
    /// Obtém questões filtradas por carreira, banca, disciplina e status.
    /// </summary>
    /// <param name="carreiraId">Identificador da carreira.</param>
    /// <param name="bancaId">Identificador da banca, ou <c>null</c> para não filtrar por banca.</param>
    /// <param name="disciplinaId">Identificador da disciplina, ou <c>null</c> para não filtrar por disciplina.</param>
    /// <param name="status">Status de revisão, ou <c>null</c> para não filtrar por status.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura de questões que atendem aos filtros informados.</returns>
    Task<IReadOnlyList<Questao>> GetByFiltroAsync(
        Guid carreiraId,
        Guid? bancaId,
        Guid? disciplinaId,
        StatusQuestao? status,
        CancellationToken cancellationToken = default);
}
