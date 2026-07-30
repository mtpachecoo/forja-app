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
    public async Task<IReadOnlyList<RespostaDisciplina>> GetHistoricoPorDisciplinaAsync(Guid usuarioId, int limitePorDisciplina, CancellationToken cancellationToken = default)
    {
        // ROW_NUMBER() OVER (PARTITION BY ...) não tem tradução LINQ portável no EF Core — SQL cru via
        // Database.SqlQuery é o mesmo padrão já usado em BuscarPorSimilaridadeAsync para o que o LINQ
        // não alcança. Sem isso, o único jeito seria carregar o histórico inteiro do usuário (pode ser
        // milhares de linhas ao longo do tempo) só para descartar quase tudo em memória.
        // Os nomes das colunas abaixo (sem alias) já batem com a convenção snake_case do ForjaDbContext
        // (UseSnakeCaseNamingConvention()) usada pelo Database.SqlQuery<T> para mapear resultado ->
        // propriedade de RespostaDisciplina — DisciplinaId/Correta/CriadoEm viram disciplina_id/correta/criado_em.
        return await Context.Database
            .SqlQuery<RespostaDisciplina>($"""
                SELECT disciplina_id, correta, criado_em
                FROM (
                    SELECT q.disciplina_id, ru.correta, ru.criado_em,
                           row_number() OVER (PARTITION BY q.disciplina_id ORDER BY ru.criado_em DESC) AS rn
                    FROM respostas_usuario ru
                    JOIN questoes q ON q.id = ru.questao_id
                    WHERE ru.usuario_id = {usuarioId}
                ) ranked
                WHERE rn <= {limitePorDisciplina}
                """)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> ContarRespostasNoDiaAsync(Guid usuarioId, DateOnly dia, CancellationToken cancellationToken = default)
    {
        var inicio = dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        return Context.RespostasUsuario
            .AsNoTracking()
            .CountAsync(r => r.UsuarioId == usuarioId && r.CriadoEm >= inicio && r.CriadoEm < fim, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RespostaUsuario>> GetUltimasAsync(Guid usuarioId, int quantidade, CancellationToken cancellationToken = default)
    {
        return await Context.RespostasUsuario
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.CriadoEm)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }
}
