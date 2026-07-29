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

    /// <summary>
    /// Obtém os chunks da(s) fonte(s) de conteúdo do tipo <see cref="TipoFonte.Edital"/> associadas a
    /// um edital — usado para tentar extrair via IA a distribuição de questões por disciplina (RF-003).
    /// </summary>
    /// <param name="editalId">Identificador do edital.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos chunks do edital (vazia se não houver fonte indexada).</returns>
    Task<IReadOnlyList<ChunkConteudo>> GetByEditalIdAsync(Guid editalId, CancellationToken cancellationToken = default);
}
