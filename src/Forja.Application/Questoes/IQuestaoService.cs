using Forja.Application.Common;
using Forja.Domain.Questoes;

namespace Forja.Application.Questoes;

/// <summary>
/// Serviço de aplicação responsável pela navegação do banco de questões (RF-004).
/// Expõe apenas questões com status <see cref="StatusQuestao.Aprovada"/> — rascunhos, questões em
/// revisão e rejeitadas não são visíveis ao usuário final.
/// </summary>
public interface IQuestaoService
{
    /// <summary>
    /// Busca questões aprovadas filtrando por carreira, banca e disciplina. Os três filtros são
    /// independentes entre si: qualquer um deles pode ser omitido, isoladamente ou em combinação.
    /// </summary>
    /// <param name="carreiraId">Identificador da carreira, ou <c>null</c> para não filtrar por carreira.</param>
    /// <param name="bancaId">Identificador da banca, ou <c>null</c> para não filtrar por banca.</param>
    /// <param name="disciplinaId">Identificador da disciplina, ou <c>null</c> para não filtrar por disciplina.</param>
    /// <param name="skip">Quantidade de questões a pular (paginação). Padrão 0.</param>
    /// <param name="take">Quantidade máxima de questões a retornar (paginação). Padrão 50, com teto interno.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das questões aprovadas que atendem aos filtros informados, já paginada.</returns>
    Task<IReadOnlyList<Questao>> BuscarAsync(
        Guid? carreiraId,
        Guid? bancaId,
        Guid? disciplinaId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma questão aprovada pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da questão.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A questão encontrada.</returns>
    /// <exception cref="NotFoundException">
    /// Lançada quando a questão não existe ou não está aprovada.
    /// </exception>
    Task<Questao> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
}
