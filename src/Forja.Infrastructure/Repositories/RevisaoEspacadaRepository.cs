using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IRevisaoEspacadaRepository"/> baseada em EF Core.
/// </summary>
public class RevisaoEspacadaRepository : Repository<RevisaoEspacada, Guid>, IRevisaoEspacadaRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public RevisaoEspacadaRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RevisaoEspacada>> GetPendentesAsync(Guid usuarioId, DateOnly ateData, CancellationToken cancellationToken = default)
    {
        return await Context.RevisoesEspacadas
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId && r.ProximaRevisaoEm <= ateData)
            .ToListAsync(cancellationToken);
    }
}
