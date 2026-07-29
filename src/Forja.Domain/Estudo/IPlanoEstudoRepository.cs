using Forja.Domain.Common;

namespace Forja.Domain.Estudo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="PlanoEstudo"/>.
/// </summary>
public interface IPlanoEstudoRepository : IRepository<PlanoEstudo, Guid>
{
    /// <summary>
    /// Obtém os planos de estudo de um usuário.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura de planos de estudo do usuário.</returns>
    Task<IReadOnlyList<PlanoEstudo>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os planos de estudo de uma carreira — usado para restringir o ranking semanal (RF-013)
    /// aos usuários que estudam para essa carreira.
    /// </summary>
    /// <param name="carreiraId">Identificador da carreira.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos planos de estudo da carreira.</returns>
    Task<IReadOnlyList<PlanoEstudo>> GetByCarreiraIdAsync(Guid carreiraId, CancellationToken cancellationToken = default);
}
