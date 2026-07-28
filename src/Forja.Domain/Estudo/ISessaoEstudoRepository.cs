using Forja.Domain.Common;

namespace Forja.Domain.Estudo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="SessaoEstudo"/>.
/// </summary>
public interface ISessaoEstudoRepository : IRepository<SessaoEstudo, Guid>
{
    /// <summary>
    /// Obtém as sessões de estudo de um usuário em uma data específica.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="data">Data das sessões desejadas.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura de sessões de estudo do usuário na data informada.</returns>
    Task<IReadOnlyList<SessaoEstudo>> GetByUsuarioIdEDataAsync(Guid usuarioId, DateOnly data, CancellationToken cancellationToken = default);
}
