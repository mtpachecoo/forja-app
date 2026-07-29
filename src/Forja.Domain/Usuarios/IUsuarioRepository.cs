using Forja.Domain.Common;

namespace Forja.Domain.Usuarios;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Usuario"/>.
/// </summary>
public interface IUsuarioRepository : IRepository<Usuario, Guid>
{
    /// <summary>
    /// Obtém um usuário pelo endereço de e-mail.
    /// </summary>
    /// <param name="email">Endereço de e-mail do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O usuário encontrado, ou <c>null</c> caso não exista.</returns>
    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os usuários correspondentes aos identificadores informados.
    /// </summary>
    /// <param name="ids">Identificadores dos usuários.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos usuários encontrados.</returns>
    Task<IReadOnlyList<Usuario>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
