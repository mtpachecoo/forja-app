namespace Forja.Domain.Contribuicao;

/// <summary>
/// Representa a submissão de um link de conteúdo externo (vídeo/PDF) por um usuário, associada a um
/// tópico, sujeita a moderação. Corresponde à tabela <c>contribuicoes_conteudo</c>.
/// </summary>
public class ContribuicaoConteudo
{
    /// <summary>Identificador único da contribuição.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário que submeteu a contribuição.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Identificador do tópico ao qual a contribuição se refere.</summary>
    public Guid TopicoId { get; set; }

    /// <summary>Tipo do conteúdo submetido.</summary>
    public TipoContribuicao Tipo { get; set; }

    /// <summary>Link (URL absoluta) do conteúdo externo.</summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>Status de moderação da contribuição.</summary>
    public StatusContribuicao Status { get; set; } = StatusContribuicao.EmRevisao;

    /// <summary>Data e hora de submissão.</summary>
    public DateTimeOffset CriadoEm { get; set; }

    /// <summary>Identificador do moderador que aprovou/rejeitou a contribuição, quando aplicável.</summary>
    public Guid? ModeradoPor { get; set; }

    /// <summary>Data e hora da moderação, quando aplicável.</summary>
    public DateTimeOffset? ModeradoEm { get; set; }
}
