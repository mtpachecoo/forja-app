namespace Forja.Domain.Estudo;

/// <summary>
/// Representa um ciclo de pomodoro dentro de uma sessão de estudo. Corresponde à tabela <c>pomodoros</c>.
/// </summary>
public class Pomodoro
{
    /// <summary>Identificador único do pomodoro.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador da sessão de estudo à qual o pomodoro pertence.</summary>
    public Guid SessaoId { get; set; }

    /// <summary>Data e hora de início do pomodoro.</summary>
    public DateTimeOffset IniciadoEm { get; set; }

    /// <summary>Data e hora de finalização do pomodoro, quando concluído.</summary>
    public DateTimeOffset? FinalizadoEm { get; set; }

    /// <summary>Duração prevista do pomodoro, em minutos.</summary>
    public int DuracaoPrevistaMin { get; set; } = 25;

    /// <summary>Quantidade de respostas registradas durante o ciclo.</summary>
    public int QtdRespostasNoCiclo { get; set; }

    /// <summary>Pontos concedidos ao usuário pela conclusão do ciclo.</summary>
    public int PontosConcedidos { get; set; }
}
