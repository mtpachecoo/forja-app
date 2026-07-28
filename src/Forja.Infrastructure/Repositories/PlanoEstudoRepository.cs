using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IPlanoEstudoRepository"/> baseada em EF Core.
/// </summary>
public class PlanoEstudoRepository : Repository<PlanoEstudo, Guid>, IPlanoEstudoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public PlanoEstudoRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanoEstudo>> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await Context.PlanosEstudo
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .ToListAsync(cancellationToken);
    }
}
