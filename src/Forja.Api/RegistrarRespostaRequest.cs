namespace Forja.Api;

/// <summary>
/// Corpo da requisição de <c>POST /respostas</c>. O identificador do usuário nunca vem do cliente —
/// é resolvido a partir do token autenticado.
/// </summary>
/// <param name="QuestaoId">Identificador da questão respondida.</param>
/// <param name="RespostaDada">Resposta informada pelo usuário.</param>
/// <param name="TempoRespostaMs">Tempo gasto para responder, em milissegundos.</param>
/// <param name="PomodoroId">Identificador do pomodoro em andamento, quando aplicável.</param>
/// <param name="EhRevisao">Indica se a resposta faz parte de uma revisão espaçada.</param>
public sealed record RegistrarRespostaRequest(
    Guid QuestaoId,
    string RespostaDada,
    int TempoRespostaMs,
    Guid? PomodoroId,
    bool EhRevisao);
