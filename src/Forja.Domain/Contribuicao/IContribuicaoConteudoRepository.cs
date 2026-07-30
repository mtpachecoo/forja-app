using Forja.Domain.Common;

namespace Forja.Domain.Contribuicao;

/// <summary>
/// Contrato de repositório para a entidade <see cref="ContribuicaoConteudo"/>.
/// </summary>
public interface IContribuicaoConteudoRepository : IRepository<ContribuicaoConteudo, Guid>
{
    /// <summary>
    /// Obtém as contribuições aprovadas de um tópico, mais recentes primeiro, paginado.
    /// </summary>
    /// <param name="topicoId">Identificador do tópico.</param>
    /// <param name="skip">Quantidade de posições a pular (paginação).</param>
    /// <param name="take">Quantidade máxima de posições a retornar (paginação).</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das contribuições aprovadas do tópico, já paginada.</returns>
    Task<IReadOnlyList<ContribuicaoConteudo>> GetAprovadasPorTopicoAsync(
        Guid topicoId, int skip, int take, CancellationToken cancellationToken = default);
}
