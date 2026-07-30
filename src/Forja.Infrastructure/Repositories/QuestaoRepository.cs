using Forja.Domain.Questoes;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IQuestaoRepository"/> baseada em EF Core.
/// </summary>
public class QuestaoRepository : Repository<Questao, Guid>, IQuestaoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public QuestaoRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Questao>> GetByFiltroAsync(
        Guid? carreiraId,
        Guid? bancaId,
        Guid? disciplinaId,
        StatusQuestao? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Questoes.AsNoTracking().AsQueryable();

        if (carreiraId.HasValue)
        {
            query = query.Where(q => q.CarreiraId == carreiraId.Value);
        }

        if (bancaId.HasValue)
        {
            query = query.Where(q => q.BancaId == bancaId.Value);
        }

        if (disciplinaId.HasValue)
        {
            query = query.Where(q => q.DisciplinaId == disciplinaId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        // Ordenação explícita por Id: Skip/Take exigem uma ordem determinística, sem ela o Postgres
        // não garante a mesma ordem entre chamadas e a paginação pode repetir ou pular registros.
        return await query
            .OrderBy(q => q.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> ContarAprovadasPorDisciplinaAsync(Guid carreiraId, CancellationToken cancellationToken = default)
    {
        var contagens = await Context.Questoes
            .AsNoTracking()
            .Where(q => q.CarreiraId == carreiraId && q.Status == StatusQuestao.Aprovada)
            .GroupBy(q => q.DisciplinaId)
            .Select(g => new { DisciplinaId = g.Key, Quantidade = g.Count() })
            .ToListAsync(cancellationToken);

        return contagens.ToDictionary(c => c.DisciplinaId, c => c.Quantidade);
    }
}
