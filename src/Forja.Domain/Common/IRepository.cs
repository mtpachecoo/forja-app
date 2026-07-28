namespace Forja.Domain.Common;

/// <summary>
/// Contrato genérico de repositório para entidades do domínio.
/// </summary>
/// <typeparam name="TEntity">Tipo da entidade gerenciada pelo repositório.</typeparam>
/// <typeparam name="TKey">Tipo da chave usada para identificar a entidade.</typeparam>
public interface IRepository<TEntity, in TKey> where TEntity : class
{
    /// <summary>
    /// Obtém uma entidade pelo seu identificador.
    /// </summary>
    /// <param name="id">Identificador da entidade.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A entidade encontrada, ou <c>null</c> caso não exista.</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as entidades do repositório.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura com todas as entidades.</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova entidade ao repositório.
    /// </summary>
    /// <param name="entity">Entidade a ser adicionada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca uma entidade existente como atualizada.
    /// </summary>
    /// <param name="entity">Entidade com os dados atualizados.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Marca uma entidade existente para remoção.
    /// </summary>
    /// <param name="entity">Entidade a ser removida.</param>
    void Remove(TEntity entity);
}
