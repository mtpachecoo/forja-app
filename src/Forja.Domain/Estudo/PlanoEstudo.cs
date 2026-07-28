namespace Forja.Domain.Estudo;

/// <summary>
/// Representa um plano de estudo de um usuário para uma carreira. Corresponde à tabela <c>planos_estudo</c>.
/// </summary>
public class PlanoEstudo
{
    /// <summary>Identificador único do plano de estudo.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário dono do plano.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Identificador da carreira alvo do plano.</summary>
    public Guid CarreiraId { get; set; }

    /// <summary>Indica se o plano foi gerado por inteligência artificial.</summary>
    public bool GeradoViaIA { get; set; } = true;

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
