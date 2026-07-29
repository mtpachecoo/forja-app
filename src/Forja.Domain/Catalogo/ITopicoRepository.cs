using Forja.Domain.Common;

namespace Forja.Domain.Catalogo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Topico"/>.
/// </summary>
public interface ITopicoRepository : IRepository<Topico, Guid>
{
    /// <summary>
    /// Obtém os tópicos de um edital — o catálogo real usado para aterrar (grounding) a geração do
    /// plano de estudo, garantindo que nenhum tópico fora dele seja incluído.
    /// </summary>
    /// <param name="editalId">Identificador do edital.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos tópicos do edital.</returns>
    Task<IReadOnlyList<Topico>> GetByEditalIdAsync(Guid editalId, CancellationToken cancellationToken = default);
}
