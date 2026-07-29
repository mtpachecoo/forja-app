using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IDisciplinaRepository"/> baseada em EF Core.
/// </summary>
public class DisciplinaRepository : Repository<Disciplina, Guid>, IDisciplinaRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public DisciplinaRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Disciplina>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idsList = ids.ToList();
        return await Context.Disciplinas
            .AsNoTracking()
            .Where(d => idsList.Contains(d.Id))
            .ToListAsync(cancellationToken);
    }
}
