using Forja.Domain.Conteudo;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IChunkConteudoRepository"/> baseada em EF Core.
/// </summary>
public class ChunkConteudoRepository : Repository<ChunkConteudo, Guid>, IChunkConteudoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public ChunkConteudoRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChunkConteudo>> GetByTopicoIdAsync(Guid topicoId, CancellationToken cancellationToken = default)
    {
        return await Context.ChunksConteudo
            .AsNoTracking()
            .Where(c => c.TopicoId == topicoId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChunkConteudo>> BuscarPorSimilaridadeAsync(float[] embedding, int quantidade, CancellationToken cancellationToken = default)
    {
        var vetor = new Vector(embedding);

        return await Context.ChunksConteudo
            .FromSqlInterpolated($"SELECT * FROM chunks_conteudo ORDER BY embedding <=> {vetor} LIMIT {quantidade}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChunkConteudo>> GetByEditalIdAsync(Guid editalId, CancellationToken cancellationToken = default)
    {
        var fonteIds = await Context.FontesConteudo
            .AsNoTracking()
            .Where(f => f.Tipo == TipoFonte.Edital && f.EditalId == editalId)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        if (fonteIds.Count == 0)
        {
            return [];
        }

        return await Context.ChunksConteudo
            .AsNoTracking()
            .Where(c => fonteIds.Contains(c.FonteId))
            .ToListAsync(cancellationToken);
    }
}
