using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IEditalPesoDisciplinaRepository"/> baseada em EF Core.
/// </summary>
public class EditalPesoDisciplinaRepository : IEditalPesoDisciplinaRepository
{
    private readonly ForjaDbContext _context;

    /// <summary>
    /// Cria uma nova instância do repositório.
    /// </summary>
    /// <param name="context">Contexto do banco de dados.</param>
    public EditalPesoDisciplinaRepository(ForjaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EditalPesoDisciplina>> GetByEditalIdAsync(Guid editalId, CancellationToken cancellationToken = default)
    {
        return await _context.EditalPesoDisciplina
            .AsNoTracking()
            .Where(p => p.EditalId == editalId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EditalPesoDisciplina>> ObterPesosDoEditalAnteriorMaisRecenteAsync(
        Guid carreiraId,
        Guid editalAtualId,
        CancellationToken cancellationToken = default)
    {
        var editalMaisRecenteId = await _context.EditalPesoDisciplina
            .AsNoTracking()
            .Where(p => p.EditalId != editalAtualId)
            .Join(_context.Editais, p => p.EditalId, e => e.Id, (p, e) => e)
            .Where(e => e.CarreiraId == carreiraId)
            .OrderByDescending(e => e.Ano)
            .ThenByDescending(e => e.CriadoEm)
            .Select(e => (Guid?)e.Id)
            .Distinct()
            .FirstOrDefaultAsync(cancellationToken);

        if (editalMaisRecenteId is null)
        {
            return [];
        }

        return await _context.EditalPesoDisciplina
            .AsNoTracking()
            .Where(p => p.EditalId == editalMaisRecenteId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<EditalPesoDisciplina> pesos, CancellationToken cancellationToken = default)
    {
        await _context.EditalPesoDisciplina.AddRangeAsync(pesos, cancellationToken);
    }
}
