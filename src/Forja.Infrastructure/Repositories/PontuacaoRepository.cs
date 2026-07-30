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
    public async Task<IReadOnlyList<Pontuacao>> GetRankingSemanalAsync(
        DateOnly semanaReferencia,
        IReadOnlyCollection<Guid>? usuarioIds,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Pontuacoes
            .AsNoTracking()
            .Where(p => p.SemanaReferencia == semanaReferencia);

        if (usuarioIds is not null)
        {
            query = query.Where(p => usuarioIds.Contains(p.UsuarioId));
        }

        return await query
            .OrderByDescending(p => p.PontosSemanaAtual)
            .ThenBy(p => p.UsuarioId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
