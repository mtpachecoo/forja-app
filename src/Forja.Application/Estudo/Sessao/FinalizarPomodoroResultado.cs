using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;

namespace Forja.Application.Estudo;

/// <summary>
/// Resultado da finalização de um pomodoro.
/// </summary>
/// <param name="Pomodoro">O pomodoro finalizado.</param>
/// <param name="Pontuacao">
/// A pontuação atualizada do usuário, quando o pomodoro concedeu pontos (RN-011); <c>null</c> quando
/// não houve nenhuma resposta registrada durante o ciclo e, portanto, nenhum ponto foi concedido.
/// </param>
public sealed record FinalizarPomodoroResultado(Pomodoro Pomodoro, Pontuacao? Pontuacao);
