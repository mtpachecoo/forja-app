using Forja.Domain.Common;

namespace Forja.Domain.Estudo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="PlanoItem"/>.
/// </summary>
public interface IPlanoItemRepository : IRepository<PlanoItem, Guid>
{
    /// <summary>
    /// Obtém os itens de um plano de estudo, ordenados por <see cref="PlanoItem.Ordem"/>.
    /// </summary>
    /// <param name="planoId">Identificador do plano de estudo.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos itens do plano.</returns>
    Task<IReadOnlyList<PlanoItem>> GetByPlanoIdAsync(Guid planoId, CancellationToken cancellationToken = default);
}
