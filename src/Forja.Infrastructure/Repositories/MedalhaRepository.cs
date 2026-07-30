using Forja.Domain.Gamificacao;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IMedalhaRepository"/> baseada em EF Core.
/// </summary>
public class MedalhaRepository : Repository<Medalha, Guid>, IMedalhaRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public MedalhaRepository(ForjaDbContext context) : base(context)
    {
    }
}
