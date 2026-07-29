using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Serviço de sessão de estudo (RF-005).
/// </summary>
public interface ISessaoEstudoService
{
    /// <summary>
    /// Inicia uma sessão de estudo para o usuário no dia de hoje. Se já existir uma sessão para o
    /// usuário nesta data, retorna a existente em vez de criar uma duplicada.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A sessão de estudo do dia (existente ou recém-criada).</returns>
    Task<SessaoEstudo> IniciarSessaoAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
