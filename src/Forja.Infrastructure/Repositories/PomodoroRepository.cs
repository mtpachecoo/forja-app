using Forja.Domain.Estudo;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IPomodoroRepository"/> baseada em EF Core.
/// </summary>
public class PomodoroRepository : Repository<Pomodoro, Guid>, IPomodoroRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public PomodoroRepository(ForjaDbContext context) : base(context)
    {
    }
}
