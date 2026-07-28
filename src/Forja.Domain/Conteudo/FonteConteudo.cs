namespace Forja.Domain.Conteudo;

/// <summary>
/// Representa uma fonte de conteúdo (lei, edital ou prova) usada como base para chunks e questões.
/// Corresponde à tabela <c>fontes_conteudo</c>.
/// </summary>
public class FonteConteudo
{
    /// <summary>Identificador único da fonte de conteúdo.</summary>
    public Guid Id { get; set; }

    /// <summary>Tipo da fonte de conteúdo.</summary>
    public TipoFonte Tipo { get; set; }

    /// <summary>Identificador do edital relacionado, quando aplicável.</summary>
    public Guid? EditalId { get; set; }

    /// <summary>Título da fonte de conteúdo.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Caminho do arquivo original armazenado, quando aplicável.</summary>
    public string? CaminhoArquivo { get; set; }

    /// <summary>Texto extraído da fonte de conteúdo.</summary>
    public string TextoExtraido { get; set; } = string.Empty;

    /// <summary>Data e hora da versão do conteúdo.</summary>
    public DateTimeOffset VersaoEm { get; set; }

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
