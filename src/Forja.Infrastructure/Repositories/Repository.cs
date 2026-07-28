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
        return await Context.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);
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

    /// <inheritdoc />
    public void Remove(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }
}
