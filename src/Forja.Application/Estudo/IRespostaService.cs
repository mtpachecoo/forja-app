using Forja.Application.Common;

namespace Forja.Application.Estudo;

/// <summary>
/// Serviço de aplicação responsável por registrar respostas de usuários a questões, calculando
/// correção e pontuação exclusivamente no backend (RN-004, RN-008 a RN-012).
/// </summary>
public interface IRespostaService
{
    /// <summary>
    /// Registra a resposta de um usuário a uma questão.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário autenticado que respondeu.</param>
    /// <param name="questaoId">Identificador da questão respondida.</param>
    /// <param name="respostaDada">Resposta informada pelo usuário, comparada contra o gabarito.</param>
    /// <param name="tempoRespostaMs">Tempo gasto para responder, em milissegundos.</param>
    /// <param name="pomodoroId">Identificador do pomodoro em andamento, quando aplicável.</param>
    /// <param name="ehRevisao">Indica se a resposta faz parte de uma revisão espaçada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado do registro, com correção e pontuação já calculadas.</returns>
    /// <exception cref="NotFoundException">
    /// Lançada quando a questão não existe ou não está aprovada.
    /// </exception>
    Task<RegistrarRespostaResultado> RegistrarRespostaAsync(
        Guid usuarioId,
        Guid questaoId,
        string respostaDada,
        int tempoRespostaMs,
        Guid? pomodoroId,
        bool ehRevisao,
        CancellationToken cancellationToken = default);
}
