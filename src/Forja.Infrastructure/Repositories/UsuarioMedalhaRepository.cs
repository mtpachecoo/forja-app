using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IUsuarioMedalhaRepository"/> baseada em EF Core.
/// </summary>
public class UsuarioMedalhaRepository : IUsuarioMedalhaRepository
{
    private readonly ForjaDbContext _context;

    /// <summary>
    /// Cria uma nova instância do repositório.
    /// </summary>
    /// <param name="context">Contexto do banco de dados.</param>
    public UsuarioMedalhaRepository(ForjaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(Guid usuarioId, Guid medalhaId, CancellationToken cancellationToken = default)
    {
        return _context.UsuarioMedalhas
            .AsNoTracking()
            .AnyAsync(u => u.UsuarioId == usuarioId && u.MedalhaId == medalhaId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(UsuarioMedalha usuarioMedalha, CancellationToken cancellationToken = default)
    {
        await _context.UsuarioMedalhas.AddAsync(usuarioMedalha, cancellationToken);
    }
}
