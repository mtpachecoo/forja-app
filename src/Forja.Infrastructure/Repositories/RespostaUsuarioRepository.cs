using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IRespostaUsuarioRepository"/> baseada em EF Core.
/// </summary>
public class RespostaUsuarioRepository : Repository<RespostaUsuario, Guid>, IRespostaUsuarioRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public RespostaUsuarioRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<bool> ExisteRespostaPontuadaAsync(Guid usuarioId, Guid questaoId, CancellationToken cancellationToken = default)
    {
        return Context.RespostasUsuario
            .AsNoTracking()
            .AnyAsync(r => r.UsuarioId == usuarioId && r.QuestaoId == questaoId && r.Pontuada, cancellationToken);
    }
}
