using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Serviço de ciclos de pomodoro dentro de uma sessão de estudo (RF-018).
/// </summary>
public interface IPomodoroService
{
    /// <summary>
    /// Inicia um novo pomodoro numa sessão de estudo do usuário.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário autenticado, dono da sessão.</param>
    /// <param name="sessaoId">Identificador da sessão de estudo.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O pomodoro iniciado.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">
    /// Lançada quando a sessão não existe ou não pertence ao usuário.
    /// </exception>
    Task<Pomodoro> IniciarPomodoroAsync(Guid usuarioId, Guid sessaoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finaliza um pomodoro. Concede pontos (RN-011) somente se houve pelo menos uma resposta
    /// registrada durante o ciclo — um pomodoro concluído sem nenhuma interação real não pontua.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário autenticado, dono da sessão.</param>
    /// <param name="sessaoId">Identificador da sessão de estudo.</param>
    /// <param name="pomodoroId">Identificador do pomodoro.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado da finalização, com a pontuação atualizada quando aplicável.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">
    /// Lançada quando a sessão ou o pomodoro não existem, não pertencem ao usuário, ou o pomodoro
    /// pertence a outra sessão.
    /// </exception>
    Task<FinalizarPomodoroResultado> FinalizarPomodoroAsync(Guid usuarioId, Guid sessaoId, Guid pomodoroId, CancellationToken cancellationToken = default);
}
