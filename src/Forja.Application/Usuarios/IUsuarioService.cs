using System.Security.Claims;
using Forja.Domain.Usuarios;

namespace Forja.Application.Usuarios;

/// <summary>
/// Serviço de aplicação responsável por resolver o usuário autenticado a partir de um
/// <see cref="ClaimsPrincipal"/> já validado, provisionando o registro em <see cref="Usuario"/>
/// no primeiro acesso.
/// </summary>
public interface IUsuarioService
{
    /// <summary>
    /// Resolve o usuário autenticado. Se for o primeiro acesso (token válido mas sem registro em
    /// <see cref="Usuario"/>), cria o registro a partir dos dados da identidade externa.
    /// </summary>
    /// <param name="principal">Principal autenticado, já validado pelo middleware de autenticação.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O usuário autenticado.</returns>
    /// <exception cref="UsuarioNaoAutenticadoException">
    /// Lançada quando o principal não contém uma claim <c>sub</c> válida, ou quando não existe
    /// identidade correspondente no provedor externo.
    /// </exception>
    Task<Usuario> ResolverUsuarioAutenticadoAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
