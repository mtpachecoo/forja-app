using Forja.Application.Estudo;

namespace Forja.Api;

/// <summary>Resposta de <c>POST /sessao/{sessaoId}/pomodoro/{pomodoroId}/finalizar</c>.</summary>
/// <param name="Pomodoro">O pomodoro finalizado.</param>
/// <param name="PontosTotal">Pontuação total do usuário após esta finalização, quando houve pontuação.</param>
/// <param name="PontosSemanaAtual">Pontuação da semana atual, quando houve pontuação.</param>
public sealed record FinalizarPomodoroResponse(PomodoroResponse Pomodoro, int? PontosTotal, int? PontosSemanaAtual)
{
    /// <summary>Constrói a resposta a partir do resultado do serviço.</summary>
    public static FinalizarPomodoroResponse De(FinalizarPomodoroResultado resultado) => new(
        PomodoroResponse.De(resultado.Pomodoro),
        resultado.Pontuacao?.PontosTotal,
        resultado.Pontuacao?.PontosSemanaAtual);
}
