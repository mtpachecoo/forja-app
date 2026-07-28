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
        Guid carreiraId,
        Guid? bancaId,
        Guid? disciplinaId,
        StatusQuestao? status,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Questoes.AsNoTracking().Where(q => q.CarreiraId == carreiraId);

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

        return await query.ToListAsync(cancellationToken);
    }
}
