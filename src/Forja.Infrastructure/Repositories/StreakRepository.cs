using Forja.Domain.Gamificacao;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IStreakRepository"/> baseada em EF Core.
/// </summary>
public class StreakRepository : Repository<Streak, Guid>, IStreakRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public StreakRepository(ForjaDbContext context) : base(context)
    {
    }
}
