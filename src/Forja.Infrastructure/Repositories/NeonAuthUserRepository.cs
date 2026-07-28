using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IIdentidadeExternaRepository"/> que lê a tabela <c>neon_auth.user</c>.
/// </summary>
public class NeonAuthUserRepository : IIdentidadeExternaRepository
{
    private readonly ForjaDbContext _context;

    /// <summary>
    /// Cria uma nova instância do repositório.
    /// </summary>
    /// <param name="context">Contexto do banco de dados.</param>
    public NeonAuthUserRepository(ForjaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IdentidadeExterna?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.NeonAuthUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return usuario == null ? null : new IdentidadeExterna(usuario.Id, usuario.Name, usuario.Email);
    }
}
