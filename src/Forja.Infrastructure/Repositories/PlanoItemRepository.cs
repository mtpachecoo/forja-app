using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IPlanoItemRepository"/> baseada em EF Core.
/// </summary>
public class PlanoItemRepository : Repository<PlanoItem, Guid>, IPlanoItemRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public PlanoItemRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanoItem>> GetByPlanoIdAsync(Guid planoId, CancellationToken cancellationToken = default)
    {
        return await Context.PlanoItens
            .AsNoTracking()
            .Where(p => p.PlanoId == planoId)
            .OrderBy(p => p.Ordem)
            .ToListAsync(cancellationToken);
    }
}
