namespace Forja.Domain.Conteudo;

/// <summary>
/// Representa um trecho (chunk) de conteúdo extraído de uma fonte, com seu vetor de embedding.
/// Corresponde à tabela <c>chunks_conteudo</c>.
/// </summary>
public class ChunkConteudo
{
    /// <summary>Identificador único do chunk.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador da fonte de conteúdo de origem.</summary>
    public Guid FonteId { get; set; }

    /// <summary>Identificador do tópico relacionado, quando aplicável.</summary>
    public Guid? TopicoId { get; set; }

    /// <summary>Texto do chunk.</summary>
    public string Texto { get; set; } = string.Empty;

    /// <summary>Vetor de embedding do texto, com 1024 dimensões.</summary>
    public float[] Embedding { get; set; } = [];

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
