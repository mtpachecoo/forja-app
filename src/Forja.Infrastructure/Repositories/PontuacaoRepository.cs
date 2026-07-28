using Forja.Domain.Gamificacao;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IPontuacaoRepository"/> baseada em EF Core.
/// </summary>
public class PontuacaoRepository : Repository<Pontuacao, Guid>, IPontuacaoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public PontuacaoRepository(ForjaDbContext context) : base(context)
    {
    }
}
