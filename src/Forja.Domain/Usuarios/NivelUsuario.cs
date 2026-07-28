namespace Forja.Domain.Usuarios;

/// <summary>
/// Nível de conhecimento declarado pelo usuário, usado para calibrar o plano de estudo.
/// </summary>
public enum NivelUsuario
{
    /// <summary>Corresponde a 'iniciante' no banco de dados.</summary>
    Iniciante,

    /// <summary>Corresponde a 'intermediario' no banco de dados.</summary>
    Intermediario,

    /// <summary>Corresponde a 'avancado' no banco de dados.</summary>
    Avancado
}
