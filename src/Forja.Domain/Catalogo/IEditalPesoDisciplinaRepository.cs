namespace Forja.Domain.Catalogo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="EditalPesoDisciplina"/>. Não estende
/// <see cref="Forja.Domain.Common.IRepository{TEntity, TKey}"/> porque a entidade tem chave composta
/// (<c>EditalId</c> + <c>DisciplinaId</c>), incompatível com o parâmetro de chave única genérico.
/// </summary>
public interface IEditalPesoDisciplinaRepository
{
    /// <summary>
    /// Obtém os pesos já calculados para um edital, se existirem.
    /// </summary>
    /// <param name="editalId">Identificador do edital.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos pesos do edital (vazia se ainda não calculado).</returns>
    Task<IReadOnlyList<EditalPesoDisciplina>> GetByEditalIdAsync(Guid editalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os pesos do edital mais recente (maior <see cref="Edital.Ano"/>), de uma mesma carreira,
    /// que já tenha pesos calculados — usado para herdar pesos em um edital novo sem chunk ou questão
    /// própria ainda (RN de cascata do RF-003, caso "herdado_edital_anterior").
    /// </summary>
    /// <param name="carreiraId">Identificador da carreira.</param>
    /// <param name="editalAtualId">Identificador do edital atual, excluído da busca.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos pesos do edital anterior encontrado (vazia se nenhum tiver pesos calculados).</returns>
    Task<IReadOnlyList<EditalPesoDisciplina>> ObterPesosDoEditalAnteriorMaisRecenteAsync(Guid carreiraId, Guid editalAtualId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste um lote de pesos calculados para um edital.
    /// </summary>
    /// <param name="pesos">Pesos a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task AddRangeAsync(IEnumerable<EditalPesoDisciplina> pesos, CancellationToken cancellationToken = default);
}
