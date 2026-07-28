using Forja.Domain.Common;

namespace Forja.Domain.Conteudo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="ChunkConteudo"/>.
/// </summary>
public interface IChunkConteudoRepository : IRepository<ChunkConteudo, Guid>
{
    /// <summary>
    /// Obtém os chunks de conteúdo associados a um tópico.
    /// </summary>
    /// <param name="topicoId">Identificador do tópico.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura de chunks associados ao tópico.</returns>
    Task<IReadOnlyList<ChunkConteudo>> GetByTopicoIdAsync(Guid topicoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca os chunks de conteúdo mais similares a um vetor de embedding de referência.
    /// </summary>
    /// <param name="embedding">Vetor de embedding de referência para a busca de similaridade.</param>
    /// <param name="quantidade">Quantidade máxima de chunks a retornar.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura de chunks ordenados por similaridade.</returns>
    Task<IReadOnlyList<ChunkConteudo>> BuscarPorSimilaridadeAsync(float[] embedding, int quantidade, CancellationToken cancellationToken = default);
}
