namespace Forja.Application.Usuarios;

/// <summary>
/// Lançada quando não é possível resolver um usuário autenticado a partir do <see cref="System.Security.Claims.ClaimsPrincipal"/>
/// informado — claim <c>sub</c> ausente, com valor inválido, ou sem identidade correspondente no provedor externo.
/// </summary>
public class UsuarioNaoAutenticadoException : Exception
{
    /// <summary>
    /// Cria uma nova instância da exceção.
    /// </summary>
    /// <param name="message">Mensagem descrevendo o motivo da falha.</param>
    public UsuarioNaoAutenticadoException(string message) : base(message)
    {
    }
}
