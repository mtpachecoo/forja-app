namespace Forja.Infrastructure.ExternalAuth;

/// <summary>
/// Mapeamento de leitura da tabela <c>neon_auth.user</c>, gerenciada externamente pelo Neon Auth.
/// Não é uma entidade do Domain — existe apenas para permitir consultas de leitura a partir da
/// Infrastructure (ex.: resolver nome/e-mail no primeiro acesso de um usuário).
/// </summary>
public class NeonAuthUser
{
    /// <summary>Identificador do usuário no Neon Auth. Mesmo valor de <c>public.usuarios.id</c>.</summary>
    public Guid Id { get; set; }

    /// <summary>Nome do usuário cadastrado no Neon Auth.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>E-mail do usuário cadastrado no Neon Auth.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Indica se o e-mail foi verificado.</summary>
    public bool EmailVerified { get; set; }
}
