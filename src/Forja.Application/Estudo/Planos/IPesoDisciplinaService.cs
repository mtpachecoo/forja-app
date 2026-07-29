namespace Forja.Application.Estudo;

/// <summary>
/// Serviço responsável por obter (calculando na primeira vez, se necessário) o peso de cada disciplina
/// dentro de um edital — insumo do RF-003 para priorizar a geração do plano de estudo. O cálculo é
/// feito uma única vez por edital, nunca por plano gerado, seguindo uma cascata de quatro estratégias
/// (ver <see cref="PesoDisciplinaService"/>).
/// </summary>
public interface IPesoDisciplinaService
{
    /// <summary>
    /// Obtém os pesos de disciplina de um edital, calculando-os pela primeira vez se ainda não existirem.
    /// </summary>
    /// <param name="editalId">Identificador do edital.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura com o peso de cada disciplina relevante ao edital.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">Lançada quando o edital não existe.</exception>
    Task<IReadOnlyList<PesoDisciplina>> ObterOuCalcularPesosAsync(Guid editalId, CancellationToken cancellationToken = default);
}
