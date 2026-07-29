using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;

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

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pontuacao>> GetRankingSemanalAsync(DateOnly semanaReferencia, CancellationToken cancellationToken = default)
    {
        return await Context.Pontuacoes
            .AsNoTracking()
            .Where(p => p.SemanaReferencia == semanaReferencia)
            .OrderByDescending(p => p.PontosSemanaAtual)
            .ToListAsync(cancellationToken);
    }
}
