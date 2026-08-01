using System.Linq.Expressions;
using Forja.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação genérica de <see cref="IRepository{TEntity, TKey}"/> baseada em EF Core.
/// </summary>
/// <typeparam name="TEntity">Tipo da entidade gerenciada pelo repositório.</typeparam>
/// <typeparam name="TKey">Tipo da chave usada para identificar a entidade.</typeparam>
public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>Contexto do banco de dados usado pelo repositório.</summary>
    protected readonly ForjaDbContext Context;

    /// <summary>
    /// Nome da propriedade de chave primária de <typeparamref name="TEntity"/> no modelo do EF Core —
    /// resolvido uma vez e cacheado, já que não muda entre instâncias (nem toda entidade usa "Id":
    /// <c>Streak</c>/<c>Pontuacao</c> usam <c>UsuarioId</c> como chave).
    /// </summary>
    private static string? _nomeDaPropriedadeChave;

    /// <summary>
    /// Cria uma nova instância do repositório.
    /// </summary>
    /// <param name="context">Contexto do banco de dados.</param>
    public Repository(ForjaDbContext context)
    {
        Context = context;
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // GetByIdAsync é usado majoritariamente para leitura pura (ex.: resolver o usuário autenticado
        // em todo request) — AsNoTracking() evita o custo de rastreamento do change tracker nesses
        // casos. Os poucos fluxos que mutam a entidade depois (ex.: PomodoroService, StreakService) já
        // chamam Update(...) explicitamente, que funciona independente do estado de tracking anterior.
        var predicado = MontarPredicadoPorChave(id);
        return await Context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(predicado, cancellationToken);
    }

    private Expression<Func<TEntity, bool>> MontarPredicadoPorChave(TKey id)
    {
        _nomeDaPropriedadeChave ??= Context.Model
            .FindEntityType(typeof(TEntity))!
            .FindPrimaryKey()!
            .Properties[0].Name;

        var entidade = Expression.Parameter(typeof(TEntity), "entidade");
        var acessoAChave = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(TKey)],
            entidade,
            Expression.Constant(_nomeDaPropriedadeChave));
        var comparacao = Expression.Equal(acessoAChave, Expression.Constant(id, typeof(TKey)));

        return Expression.Lambda<Func<TEntity, bool>>(comparacao, entidade);
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }
}
