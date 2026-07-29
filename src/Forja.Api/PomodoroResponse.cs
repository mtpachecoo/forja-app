using Forja.Domain.Estudo;

namespace Forja.Api;

/// <summary>Resposta com os dados de um pomodoro.</summary>
/// <param name="Id">Identificador do pomodoro.</param>
/// <param name="SessaoId">Identificador da sessão de estudo à qual pertence.</param>
/// <param name="IniciadoEm">Data e hora de início.</param>
/// <param name="FinalizadoEm">Data e hora de finalização, quando concluído.</param>
/// <param name="DuracaoPrevistaMin">Duração prevista, em minutos.</param>
/// <param name="QtdRespostasNoCiclo">Quantidade de respostas registradas durante o ciclo.</param>
/// <param name="PontosConcedidos">Pontos concedidos pela conclusão do ciclo.</param>
public sealed record PomodoroResponse(
    Guid Id,
    Guid SessaoId,
    DateTimeOffset IniciadoEm,
    DateTimeOffset? FinalizadoEm,
    int DuracaoPrevistaMin,
    int QtdRespostasNoCiclo,
    int PontosConcedidos)
{
    /// <summary>Constrói a resposta a partir da entidade de domínio.</summary>
    public static PomodoroResponse De(Pomodoro pomodoro) => new(
        pomodoro.Id,
        pomodoro.SessaoId,
        pomodoro.IniciadoEm,
        pomodoro.FinalizadoEm,
        pomodoro.DuracaoPrevistaMin,
        pomodoro.QtdRespostasNoCiclo,
        pomodoro.PontosConcedidos);
}
