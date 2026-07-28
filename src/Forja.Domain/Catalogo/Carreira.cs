namespace Forja.Domain.Catalogo;

/// <summary>
/// Representa uma carreira pública para a qual há preparação disponível. Corresponde à tabela <c>carreiras</c>.
/// </summary>
public class Carreira
{
    /// <summary>Identificador único da carreira.</summary>
    public Guid Id { get; set; }

    /// <summary>Nome da carreira.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Órgão responsável pelo concurso/carreira.</summary>
    public string Orgao { get; set; } = string.Empty;

    /// <summary>Cargo específico dentro da carreira, quando aplicável.</summary>
    public string? Cargo { get; set; }

    /// <summary>Indica se a carreira está ativa e visível no catálogo.</summary>
    public bool AtivoNoCatalogo { get; set; } = true;

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
