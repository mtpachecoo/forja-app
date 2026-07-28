using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IUsuarioRepository"/> baseada em EF Core.
/// </summary>
public class UsuarioRepository : Repository<Usuario, Guid>, IUsuarioRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public UsuarioRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}
