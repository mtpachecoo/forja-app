using Forja.Domain.Common;

namespace Forja.Domain.Estudo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="RevisaoEspacada"/>.
/// </summary>
public interface IRevisaoEspacadaRepository : IRepository<RevisaoEspacada, Guid>
{
    /// <summary>
    /// Obtém os registros de revisão espaçada de um usuário com previsão até uma data limite.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="ateData">Data limite (inclusive) para a próxima revisão.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura de revisões pendentes até a data informada.</returns>
    Task<IReadOnlyList<RevisaoEspacada>> GetPendentesAsync(Guid usuarioId, DateOnly ateData, CancellationToken cancellationToken = default);
}
