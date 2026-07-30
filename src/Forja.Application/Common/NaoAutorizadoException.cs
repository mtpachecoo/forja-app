namespace Forja.Application.Common;

/// <summary>
/// Lançada quando um usuário autenticado tenta executar uma ação pra qual não tem permissão (distinto
/// de não estar autenticado, ver <see cref="Forja.Application.Usuarios.UsuarioNaoAutenticadoException"/>).
/// Genérica pra todos os casos de uso da Application.
/// </summary>
public class NaoAutorizadoException : Exception
{
    /// <summary>
    /// Cria uma nova instância da exceção.
    /// </summary>
    /// <param name="message">Mensagem descrevendo o motivo da recusa.</param>
    public NaoAutorizadoException(string message) : base(message)
    {
    }
}
