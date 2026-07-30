using Forja.Domain.Contribuicao;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IContribuicaoConteudoRepository"/> baseada em EF Core.
/// </summary>
public class ContribuicaoConteudoRepository : Repository<ContribuicaoConteudo, Guid>, IContribuicaoConteudoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public ContribuicaoConteudoRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContribuicaoConteudo>> GetAprovadasPorTopicoAsync(
        Guid topicoId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await Context.ContribuicoesConteudo
            .AsNoTracking()
            .Where(c => c.TopicoId == topicoId && c.Status == StatusContribuicao.Aprovada)
            .OrderByDescending(c => c.CriadoEm)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
