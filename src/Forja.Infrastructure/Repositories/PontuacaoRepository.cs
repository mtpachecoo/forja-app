using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure.Repositories;

/// <summary>
/// Implementação de <see cref="IPontuacaoRepository"/> baseada em EF Core.
/// </summary>
public class PontuacaoRepository : Repository<Pontuacao, Guid>, IPontuacaoRepository
{
    /// <inheritdoc cref="Repository{TEntity, TKey}(ForjaDbContext)" />
    public PontuacaoRepository(ForjaDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Pontuacao>> GetRankingSemanalAsync(
        DateOnly semanaReferencia,
        IReadOnlyCollection<Guid>? usuarioIds,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Pontuacoes
            .AsNoTracking()
            .Where(p => p.SemanaReferencia == semanaReferencia);

        if (usuarioIds is not null)
        {
            query = query.Where(p => usuarioIds.Contains(p.UsuarioId));
        }

        return await query
            .OrderByDescending(p => p.PontosSemanaAtual)
            .ThenBy(p => p.UsuarioId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Pontuacao> IncrementarPontosAsync(Guid usuarioId, int pontos, DateOnly hoje, CancellationToken cancellationToken = default)
    {
        var inicioSemana = Pontuacao.InicioDaSemana(hoje);

        // Upsert atômico: se a linha não existe, INSERT com os pontos desta chamada; se existe, soma
        // em cima do valor já gravado no próprio Postgres (não em memória) — a leitura e a escrita são
        // uma única instrução, então duas chamadas concorrentes pro mesmo usuário não podem se
        // sobrescrever. pontuacoes.semana_referencia (qualificado com o nome da tabela) refere-se à
        // linha existente sendo atualizada, não ao valor recém-inserido.
        // Um INSERT não é "composable" — EF Core normalmente tenta compor SQL adicional em cima da
        // query pra resolver operadores como Single()/First(), o que falha pra uma instrução de escrita.
        // ToListAsync() executa a instrução como está, sem tentar compor nada; Single() sobre a lista
        // resultante (já em memória, uma linha só) resolve o resultado sem esse problema.
        var linhas = await Context.Database.SqlQuery<PontuacaoAtualizada>($"""
            INSERT INTO pontuacoes (usuario_id, pontos_total, pontos_semana_atual, semana_referencia)
            VALUES ({usuarioId}, {pontos}, {pontos}, {inicioSemana})
            ON CONFLICT (usuario_id) DO UPDATE SET
                pontos_total = pontuacoes.pontos_total + {pontos},
                pontos_semana_atual = CASE
                    WHEN pontuacoes.semana_referencia = {inicioSemana} THEN pontuacoes.pontos_semana_atual + {pontos}
                    ELSE {pontos}
                END,
                semana_referencia = {inicioSemana}
            RETURNING usuario_id, pontos_total, pontos_semana_atual, semana_referencia
            """)
            .ToListAsync(cancellationToken);
        var linha = linhas.Single();

        return new Pontuacao
        {
            UsuarioId = linha.UsuarioId,
            PontosTotal = linha.PontosTotal,
            PontosSemanaAtual = linha.PontosSemanaAtual,
            SemanaReferencia = linha.SemanaReferencia,
        };
    }

    /// <summary>Projeção da linha retornada pelo upsert de <see cref="IncrementarPontosAsync"/>.</summary>
    private sealed record PontuacaoAtualizada(Guid UsuarioId, int PontosTotal, int PontosSemanaAtual, DateOnly SemanaReferencia);
}
