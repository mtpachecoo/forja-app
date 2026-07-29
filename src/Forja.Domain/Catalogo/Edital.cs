namespace Forja.Domain.Catalogo;

/// <summary>
/// Representa um edital de concurso público. Corresponde à tabela <c>editais</c>.
/// </summary>
public class Edital
{
    /// <summary>Identificador único do edital.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador da carreira à qual o edital se refere.</summary>
    public Guid CarreiraId { get; set; }

    /// <summary>Identificador da banca organizadora do concurso.</summary>
    public Guid BancaId { get; set; }

    /// <summary>Ano de publicação do edital.</summary>
    public int Ano { get; set; }

    /// <summary>Data da prova, quando já divulgada. <c>null</c> se o concurso ainda não tem data marcada.</summary>
    public DateOnly? DataProva { get; set; }

    /// <summary>URL da fonte original do edital.</summary>
    public string? UrlFonte { get; set; }

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
