using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="ITopicoRepository"/> baseada em EF Core.
/// </summary>
public class TopicoRepository : Repository<Topico, Guid>, ITopicoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public TopicoRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Topico>> GetByEditalIdAsync(Guid editalId, CancellationToken cancellationToken = default)
    {
        return await Context.Topicos
            .AsNoTracking()
            .Where(t => t.EditalId == editalId)
            .OrderBy(t => t.Ordem)
            .ToListAsync(cancellationToken);
    }
}
