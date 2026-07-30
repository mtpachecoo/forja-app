using Forja.Domain.Contribuicao;
using Forja.Domain.Gamificacao;
using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.ReadStores;

/// <summary>
/// Implementação de <see cref="IPerfilPublicoReadStore"/>. Consulta <see cref="ForjaDbContext"/>
/// diretamente com uma projeção explícita — sem carregar a entidade <see cref="Usuario"/> inteira nem
/// passar pelo <see cref="Domain.Common.IRepository{TEntity,TKey}"/> genérico, que traria a entidade
/// completa (incluindo campos privados) só pra descartar a maior parte em memória.
/// </summary>
public class PerfilPublicoReadStore : IPerfilPublicoReadStore
{
    private readonly ForjaDbContext _context;

    /// <summary>
    /// Cria uma nova instância do read store.
    /// </summary>
    /// <param name="context">Contexto do banco de dados.</param>
    public PerfilPublicoReadStore(ForjaDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PerfilPublico?> ObterPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.Id, u.Nome })
            .FirstOrDefaultAsync(cancellationToken);

        if (usuario is null)
        {
            return null;
        }

        var badges = await _context.UsuarioMedalhas
            .AsNoTracking()
            .Where(um => um.UsuarioId == usuarioId)
            .Join(_context.Medalhas, um => um.MedalhaId, m => m.Id, (um, m) => m.Nome)
            .ToListAsync(cancellationToken);

        var contribuicoesAprovadas = await _context.ContribuicoesConteudo
            .AsNoTracking()
            .CountAsync(c => c.UsuarioId == usuarioId && c.Status == StatusContribuicao.Aprovada, cancellationToken);

        var pontosContribuicao = await _context.ReputacoesContribuicao
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId)
            .Select(r => (int?)r.PontosContribuicao)
            .FirstOrDefaultAsync(cancellationToken);

        return new PerfilPublico(
            usuario.Id,
            usuario.Nome,
            badges,
            contribuicoesAprovadas,
            ReputacaoCalculator.CalcularNivel(pontosContribuicao ?? 0));
    }
}
