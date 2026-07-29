namespace Forja.Application.Estudo;

/// <summary>
/// Orquestra o registro de uma resposta e os efeitos colaterais que dependem dela — hoje, a
/// atualização da revisão espaçada da questão (RN-003/RF-010). Existe para que <c>Program.cs</c> não
/// precise encadear múltiplos serviços diretamente no endpoint <c>POST /respostas</c>.
/// </summary>
public interface IRegistrarRespostaComEfeitosService
{
    /// <summary>
    /// Registra a resposta via <see cref="IRespostaService"/> e, em seguida, atualiza a revisão
    /// espaçada da questão via <see cref="IRevisaoEspacadaService"/> com o resultado da correção.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário autenticado que respondeu.</param>
    /// <param name="questaoId">Identificador da questão respondida.</param>
    /// <param name="respostaDada">Resposta informada pelo usuário, comparada contra o gabarito.</param>
    /// <param name="tempoRespostaMs">Tempo gasto para responder, em milissegundos.</param>
    /// <param name="pomodoroId">Identificador do pomodoro em andamento, quando aplicável.</param>
    /// <param name="ehRevisao">Indica se a resposta faz parte de uma revisão espaçada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O resultado do registro da resposta, com correção e pontuação já calculadas.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">
    /// Lançada quando a questão não existe ou não está aprovada.
    /// </exception>
    Task<RegistrarRespostaResultado> RegistrarAsync(
        Guid usuarioId,
        Guid questaoId,
        string respostaDada,
        int tempoRespostaMs,
        Guid? pomodoroId,
        bool ehRevisao,
        CancellationToken cancellationToken = default);
}
