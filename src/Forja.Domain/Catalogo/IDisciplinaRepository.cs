using Forja.Domain.Common;

namespace Forja.Domain.Catalogo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Disciplina"/>.
/// </summary>
public interface IDisciplinaRepository : IRepository<Disciplina, Guid>
{
    /// <summary>
    /// Obtém as disciplinas correspondentes aos identificadores informados.
    /// </summary>
    /// <param name="ids">Identificadores das disciplinas.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das disciplinas encontradas.</returns>
    Task<IReadOnlyList<Disciplina>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
