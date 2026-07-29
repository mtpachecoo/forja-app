namespace Forja.Application.Duvidas;

/// <summary>
/// Porta para geração de embeddings de texto. A implementação deve usar o mesmo provedor/modelo
/// (Voyage AI) já usado pelo Motor de Conteúdo (Python) na ingestão de <c>chunks_conteudo</c> —
/// embeddings de modelos diferentes não são comparáveis por similaridade de cosseno, mesmo tendo a
/// mesma dimensão.
/// </summary>
public interface IGeradorDeEmbeddings
{
    /// <summary>
    /// Gera o vetor de embedding de um texto.
    /// </summary>
    /// <param name="texto">Texto a ser embedado.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Vetor de embedding do texto.</returns>
    Task<float[]> GerarEmbeddingAsync(string texto, CancellationToken cancellationToken = default);
}
