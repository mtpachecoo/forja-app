namespace Forja.Domain.Common;

/// <summary>
/// Contrato para persistir, em uma única transação, as mudanças rastreadas pelos repositórios.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persiste todas as mudanças pendentes.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Quantidade de registros afetados.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
