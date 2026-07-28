namespace Forja.Domain.Usuarios;

/// <summary>
/// Representa um usuário da plataforma. Corresponde à tabela <c>usuarios</c>.
/// </summary>
public class Usuario
{
    /// <summary>Identificador único do usuário.</summary>
    public Guid Id { get; set; }

    /// <summary>Endereço de e-mail do usuário. Único no sistema.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Nome do usuário.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Nível de conhecimento declarado pelo usuário.</summary>
    public NivelUsuario Nivel { get; set; }

    /// <summary>Tempo disponível por dia para estudo, em minutos.</summary>
    public int TempoDisponivelMinDia { get; set; }

    /// <summary>Fuso horário do usuário (identificador IANA, ex.: "America/Sao_Paulo").</summary>
    public string FusoHorario { get; set; } = "America/Sao_Paulo";

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }
}
