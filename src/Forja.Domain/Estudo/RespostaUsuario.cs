namespace Forja.Domain.Estudo;

/// <summary>
/// Representa a resposta de um usuário a uma questão. Corresponde à tabela <c>respostas_usuario</c>.
/// </summary>
public class RespostaUsuario
{
    /// <summary>Identificador único da resposta.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário que respondeu.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Identificador da questão respondida.</summary>
    public Guid QuestaoId { get; set; }

    /// <summary>Identificador do pomodoro durante o qual a resposta foi dada, quando aplicável.</summary>
    public Guid? PomodoroId { get; set; }

    /// <summary>Resposta dada pelo usuário.</summary>
    public string RespostaDada { get; set; } = string.Empty;

    /// <summary>Indica se a resposta está correta.</summary>
    public bool Correta { get; set; }

    /// <summary>Tempo gasto para responder, em milissegundos.</summary>
    public int TempoRespostaMs { get; set; }

    /// <summary>Indica se a resposta gerou pontuação.</summary>
    public bool Pontuada { get; set; }

    /// <summary>Pontos concedidos ao usuário por essa resposta.</summary>
    public int PontosConcedidos { get; set; }

    /// <summary>Indica se a resposta faz parte de uma revisão espaçada.</summary>
    public bool EhRevisao { get; set; }

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
