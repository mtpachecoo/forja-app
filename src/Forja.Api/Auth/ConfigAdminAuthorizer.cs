using Forja.Application.Common;

namespace Forja.Api.Auth;

/// <summary>
/// Implementação de <see cref="IAdminAuthorizer"/> baseada numa allowlist de e-mails em config
/// (<c>Admin:Emails</c>, ex.: <c>Admin__Emails__0</c> no <c>.env</c>).
/// </summary>
public class ConfigAdminAuthorizer : IAdminAuthorizer
{
    private readonly HashSet<string> _emailsAdmin;

    /// <summary>
    /// Cria uma nova instância do autorizador.
    /// </summary>
    /// <param name="configuration">Configuração da aplicação, contendo a seção <c>Admin:Emails</c>.</param>
    public ConfigAdminAuthorizer(IConfiguration configuration)
    {
        var emails = configuration.GetSection("Admin:Emails").Get<string[]>() ?? [];
        _emailsAdmin = new HashSet<string>(emails, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool EhAdmin(string email) => _emailsAdmin.Contains(email);
}
