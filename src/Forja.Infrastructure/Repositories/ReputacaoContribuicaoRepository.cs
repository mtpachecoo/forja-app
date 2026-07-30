using Forja.Domain.Gamificacao;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IReputacaoContribuicaoRepository"/> baseada em EF Core.
/// </summary>
public class ReputacaoContribuicaoRepository : Repository<ReputacaoContribuicao, Guid>, IReputacaoContribuicaoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public ReputacaoContribuicaoRepository(ForjaDbContext context) : base(context)
    {
    }
}
