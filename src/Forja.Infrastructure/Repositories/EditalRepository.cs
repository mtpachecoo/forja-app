using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IEditalRepository"/> baseada em EF Core.
/// </summary>
public class EditalRepository : Repository<Edital, Guid>, IEditalRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public EditalRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Edital?> GetMaisRecentePorCarreiraAsync(Guid carreiraId, CancellationToken cancellationToken = default)
    {
        return await Context.Editais
            .AsNoTracking()
            .Where(e => e.CarreiraId == carreiraId)
            .OrderByDescending(e => e.Ano)
            .ThenByDescending(e => e.CriadoEm)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
