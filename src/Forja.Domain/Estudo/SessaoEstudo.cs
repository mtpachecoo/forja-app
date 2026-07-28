namespace Forja.Domain.Estudo;

/// <summary>
/// Representa uma sessão de estudo diária de um usuário. Corresponde à tabela <c>sessoes_estudo</c>.
/// </summary>
public class SessaoEstudo
{
    /// <summary>Identificador único da sessão de estudo.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário dono da sessão.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Data em que a sessão de estudo ocorreu.</summary>
    public DateOnly DataSessao { get; set; }

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
