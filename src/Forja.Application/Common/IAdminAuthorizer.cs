namespace Forja.Application.Common;

/// <summary>
/// Verifica se um e-mail pertence a um administrador. Implementação de referência
/// (<c>ConfigAdminAuthorizer</c>, em Forja.Infrastructure) lê uma allowlist de config
/// (<c>Admin:Emails</c>) — não há conceito de role/organização hoje; essa é a forma mais simples e
/// testável de restringir moderação sem inventar infraestrutura de autorização nova.
/// </summary>
public interface IAdminAuthorizer
{
    /// <summary>
    /// Verifica se o e-mail informado pertence a um administrador.
    /// </summary>
    /// <param name="email">E-mail a verificar.</param>
    /// <returns><c>true</c> se o e-mail está na allowlist de administradores.</returns>
    bool EhAdmin(string email);
}
