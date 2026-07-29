using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Serviço de revisão espaçada (RF-010). Mantém, por usuário e questão, o intervalo até a próxima
/// revisão: acerto aumenta o intervalo (revisa mais tarde); erro consecutivo reseta o intervalo para
/// o mínimo (revisa de novo mais cedo) — RN-003.
/// </summary>
public interface IRevisaoEspacadaService
{
    /// <summary>
    /// Obtém a fila de revisão pendente do usuário — questões cuja próxima revisão já venceu.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das revisões pendentes.</returns>
    Task<IReadOnlyList<RevisaoEspacada>> ObterPendentesAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza o estado de revisão espaçada de uma questão a partir do resultado de uma resposta.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="questaoId">Identificador da questão respondida.</param>
    /// <param name="correta">Indica se a resposta estava correta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O estado de revisão espaçada atualizado.</returns>
    Task<RevisaoEspacada> RegistrarRespostaAsync(Guid usuarioId, Guid questaoId, bool correta, CancellationToken cancellationToken = default);
}
