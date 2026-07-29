using Forja.Domain.Common;

namespace Forja.Domain.Catalogo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Edital"/>.
/// </summary>
public interface IEditalRepository : IRepository<Edital, Guid>
{
    /// <summary>
    /// Obtém o edital mais recente (maior <see cref="Edital.Ano"/>) de uma carreira.
    /// </summary>
    /// <param name="carreiraId">Identificador da carreira.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O edital mais recente da carreira, ou <c>null</c> se não houver nenhum.</returns>
    Task<Edital?> GetMaisRecentePorCarreiraAsync(Guid carreiraId, CancellationToken cancellationToken = default);
}
