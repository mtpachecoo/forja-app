using Forja.Domain.Common;

namespace Forja.Infrastructure;

/// <summary>
/// Implementação de <see cref="IUnitOfWork"/> baseada em <see cref="ForjaDbContext"/>.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ForjaDbContext _context;

    /// <summary>
    /// Cria uma nova instância do Unit of Work.
    /// </summary>
    /// <param name="context">Contexto do banco de dados.</param>
    public UnitOfWork(ForjaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
