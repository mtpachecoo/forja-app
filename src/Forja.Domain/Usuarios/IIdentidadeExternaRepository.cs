namespace Forja.Domain.Usuarios;

/// <summary>
/// Contrato de leitura para consultar a identidade de um usuário em um provedor de autenticação externo.
/// </summary>
public interface IIdentidadeExternaRepository
{
    /// <summary>
    /// Obtém a identidade externa de um usuário pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A identidade encontrada, ou <c>null</c> caso não exista.</returns>
    Task<IdentidadeExterna?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
