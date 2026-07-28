using System.Security.Claims;
using Forja.Domain.Common;
using Forja.Domain.Usuarios;

namespace Forja.Application.Usuarios;

/// <summary>
/// Implementação padrão de <see cref="IUsuarioService"/>.
/// </summary>
public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IIdentidadeExternaRepository _identidadeExternaRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    /// <param name="usuarioRepository">Repositório de usuários.</param>
    /// <param name="identidadeExternaRepository">Repositório de identidade externa (provedor de autenticação).</param>
    /// <param name="unitOfWork">Unit of work para persistir o provisionamento do usuário.</param>
    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IIdentidadeExternaRepository identidadeExternaRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _identidadeExternaRepository = identidadeExternaRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Usuario> ResolverUsuarioAutenticadoAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var subClaim = principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var usuarioId))
        {
            throw new UsuarioNaoAutenticadoException("Token sem claim 'sub' válida.");
        }

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario != null)
        {
            return usuario;
        }

        var identidadeExterna = await _identidadeExternaRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (identidadeExterna == null)
        {
            throw new UsuarioNaoAutenticadoException($"Nenhuma identidade externa encontrada para o usuário '{usuarioId}'.");
        }

        var novoUsuario = new Usuario
        {
            Id = identidadeExterna.Id,
            Nome = identidadeExterna.Nome,
            Email = identidadeExterna.Email,
            Nivel = NivelUsuario.Iniciante,
            TempoDisponivelMinDia = 60,
        };

        await _usuarioRepository.AddAsync(novoUsuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return novoUsuario;
    }
}
