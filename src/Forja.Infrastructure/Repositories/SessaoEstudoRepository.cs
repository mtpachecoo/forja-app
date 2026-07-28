using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="ISessaoEstudoRepository"/> baseada em EF Core.
/// </summary>
public class SessaoEstudoRepository : Repository<SessaoEstudo, Guid>, ISessaoEstudoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public SessaoEstudoRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessaoEstudo>> GetByUsuarioIdEDataAsync(Guid usuarioId, DateOnly data, CancellationToken cancellationToken = default)
    {
        return await Context.SessoesEstudo
            .AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId && s.DataSessao == data)
            .ToListAsync(cancellationToken);
    }
}
