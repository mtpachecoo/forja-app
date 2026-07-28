namespace Forja.Domain.Catalogo;

/// <summary>
/// Representa uma banca organizadora de concursos. Corresponde à tabela <c>bancas</c>.
/// </summary>
public class Banca
{
    /// <summary>Identificador único da banca.</summary>
    public Guid Id { get; set; }

    /// <summary>Nome da banca. Único no sistema.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
