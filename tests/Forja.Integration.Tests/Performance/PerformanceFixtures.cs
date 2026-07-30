using Forja.Domain.Catalogo;
using Forja.Domain.Questoes;
using Forja.Infrastructure;

namespace Forja.Integration.Tests.Performance;

/// <summary>
/// Helpers de seed compartilhados entre os testes de carga — mesmo espírito dos helpers protegidos de
/// <see cref="IntegrationTestBase"/>, só que específicos desta rodada de auditoria de performance.
/// </summary>
internal static class PerformanceFixtures
{
    /// <summary>Cria um edital com <paramref name="quantidadeTopicos"/> tópicos, todos da mesma disciplina.</summary>
    public static async Task<(Edital Edital, List<Topico> Topicos)> CriarEditalComTopicosAsync(
        ForjaDbContext context, Guid carreiraId, Guid disciplinaId, int quantidadeTopicos)
    {
        var banca = new Banca { Id = Guid.NewGuid(), Nome = $"Banca {Guid.NewGuid():N}", CriadoEm = DateTimeOffset.UtcNow };
        context.Bancas.Add(banca);
        var edital = new Edital { Id = Guid.NewGuid(), CarreiraId = carreiraId, BancaId = banca.Id, Ano = 2024, CriadoEm = DateTimeOffset.UtcNow };
        context.Editais.Add(edital);

        var topicos = Enumerable.Range(1, quantidadeTopicos)
            .Select(i => new Topico { Id = Guid.NewGuid(), EditalId = edital.Id, DisciplinaId = disciplinaId, Nome = $"Tópico {i}", Ordem = i })
            .ToList();
        context.Topicos.AddRange(topicos);

        await context.SaveChangesAsync();
        return (edital, topicos);
    }

    /// <summary>Cria <paramref name="quantidade"/> questões aprovadas e distintas, gabarito sempre "A".</summary>
    public static async Task<List<Questao>> CriarQuestoesAprovadasAsync(
        ForjaDbContext context, Guid carreiraId, Guid disciplinaId, int quantidade)
    {
        var questoes = Enumerable.Range(1, quantidade)
            .Select(i => new Questao
            {
                Id = Guid.NewGuid(),
                CarreiraId = carreiraId,
                DisciplinaId = disciplinaId,
                Tipo = TipoQuestao.CertoErrado,
                Enunciado = $"Enunciado {i}",
                Gabarito = "Certo",
                Explicacao = "Explicação",
                Status = StatusQuestao.Aprovada,
                CriadoEm = DateTimeOffset.UtcNow,
            })
            .ToList();

        context.Questoes.AddRange(questoes);
        await context.SaveChangesAsync();
        return questoes;
    }
}
