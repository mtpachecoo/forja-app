using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IRespostaUsuarioRepository"/> baseada em EF Core.
/// </summary>
public class RespostaUsuarioRepository : Repository<RespostaUsuario, Guid>, IRespostaUsuarioRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public RespostaUsuarioRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<bool> ExisteRespostaPontuadaAsync(Guid usuarioId, Guid questaoId, CancellationToken cancellationToken = default)
    {
        return Context.RespostasUsuario
            .AsNoTracking()
            .AnyAsync(r => r.UsuarioId == usuarioId && r.QuestaoId == questaoId && r.Pontuada, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> ContarRespostasNoPomodoroAsync(Guid pomodoroId, CancellationToken cancellationToken = default)
    {
        return Context.RespostasUsuario
            .AsNoTracking()
            .CountAsync(r => r.PomodoroId == pomodoroId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RespostaDisciplina>> GetHistoricoPorDisciplinaAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await Context.RespostasUsuario
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId)
            .Join(Context.Questoes, r => r.QuestaoId, q => q.Id, (r, q) => new RespostaDisciplina(q.DisciplinaId, r.Correta, r.CriadoEm))
            .OrderBy(rd => rd.CriadoEm)
            .ToListAsync(cancellationToken);
    }
}
