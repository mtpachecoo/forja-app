using System.Diagnostics;
using Forja.Application.Estudo;
using Forja.Application.Questoes;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Performance;

/// <summary>
/// Fluxo 3 da auditoria de performance: lote de <c>POST /respostas</c> em sequência rápida, mesmo
/// usuário. Compara um usuário "novo" (sem histórico) com um "veterano" (milhares de respostas/revisões
/// prévias já persistidas) respondendo o mesmo número de questões novas — se o tempo por resposta
/// degradar no veterano, é evidência de uma consulta O(n) no histórico do usuário (candidatos:
/// <see cref="Forja.Domain.Estudo.IRespostaUsuarioRepository.ExisteRespostaPontuadaAsync"/> e
/// <see cref="Forja.Domain.Estudo.IRevisaoEspacadaRepository.GetByUsuarioIdEQuestaoIdAsync"/> — nenhum
/// dos dois tem índice composto (usuario_id, questao_id), só os índices simples de FK).
/// </summary>
[TestClass]
public class SessaoEstudosLoadTests : IntegrationTestBase
{
    private const int RespostasPorLote = 100;
    private const int HistoricoDoVeterano = 3000;

    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static RegistrarRespostaComEfeitosService CriarServico(IServiceScope escopo) => new(
        new RespostaService(
            new QuestaoService(escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>()),
            escopo.ServiceProvider.GetRequiredService<IRespostaUsuarioRepository>(),
            escopo.ServiceProvider.GetRequiredService<Forja.Domain.Gamificacao.IPontuacaoRepository>()),
        new RevisaoEspacadaService(escopo.ServiceProvider.GetRequiredService<IRevisaoEspacadaRepository>()),
        escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

    private async Task<List<double>> ResponderLoteAsync(Guid usuarioId, IReadOnlyList<Questao> questoes)
    {
        var tempos = new List<double>();
        var cronometro = new Stopwatch();

        foreach (var questao in questoes)
        {
            using var escopo = CriarEscopo();
            var servico = CriarServico(escopo);

            cronometro.Restart();
            await servico.RegistrarAsync(usuarioId, questao.Id, "Certo", tempoRespostaMs: 8_000, pomodoroId: null, ehRevisao: false);
            cronometro.Stop();

            tempos.Add(cronometro.Elapsed.TotalMilliseconds);
        }

        return tempos;
    }

    /// <summary>
    /// Semeia histórico prévio "pesado" pro usuário veterano: milhares de respostas e revisões
    /// espaçadas já persistidas, pra questões que NÃO fazem parte do lote medido — só existem pra
    /// engordar a tabela e expor uma eventual degradação O(n) na busca das questões novas.
    /// </summary>
    private static async Task SemearHistoricoAsync(ForjaDbContext context, Guid usuarioId, Guid carreiraId, Guid disciplinaId)
    {
        var questoesAntigas = await PerformanceFixtures.CriarQuestoesAprovadasAsync(context, carreiraId, disciplinaId, HistoricoDoVeterano);

        var agora = DateTimeOffset.UtcNow;
        var respostas = questoesAntigas.Select((q, indice) => new RespostaUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            QuestaoId = q.Id,
            RespostaDada = "Certo",
            Correta = true,
            TempoRespostaMs = 8_000,
            Pontuada = true,
            PontosConcedidos = 10,
            CriadoEm = agora.AddDays(-indice % 365 - 1),
        });
        context.RespostasUsuario.AddRange(respostas);

        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);
        var revisoes = questoesAntigas.Select(q => new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            QuestaoId = q.Id,
            ErrosConsecutivos = 0,
            IntervaloDiasAtual = 2,
            ProximaRevisaoEm = hoje.AddDays(30),
        });
        context.RevisoesEspacadas.AddRange(revisoes);

        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Carga_LoteDeRespostas_UsuarioNovoVsVeterano_ComparaDegradacao()
    {
        Guid carreiraId, disciplinaId;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            carreiraId = carreira.Id;
            disciplinaId = disciplina.Id;
        }

        // Usuário novo: sem nenhum histórico prévio.
        Guid usuarioNovoId;
        List<Questao> questoesParaNovo;
        using (var escopo = CriarEscopo())
        {
            var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(context);
            usuarioNovoId = usuario.Id;
            questoesParaNovo = await PerformanceFixtures.CriarQuestoesAprovadasAsync(context, carreiraId, disciplinaId, RespostasPorLote);
        }

        var temposUsuarioNovo = await ResponderLoteAsync(usuarioNovoId, questoesParaNovo);

        // Usuário veterano: milhares de respostas/revisões prévias, depois o mesmo lote de questões novas.
        Guid usuarioVeteranoId;
        List<Questao> questoesParaVeterano;
        using (var escopo = CriarEscopo())
        {
            var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(context);
            usuarioVeteranoId = usuario.Id;
            await SemearHistoricoAsync(context, usuarioVeteranoId, carreiraId, disciplinaId);
            questoesParaVeterano = await PerformanceFixtures.CriarQuestoesAprovadasAsync(context, carreiraId, disciplinaId, RespostasPorLote);
        }

        var temposUsuarioVeterano = await ResponderLoteAsync(usuarioVeteranoId, questoesParaVeterano);

        PerformanceReport.EscreverSecao(
            "Fluxo 3a — Sessão de estudos: usuário SEM histórico prévio",
            temposUsuarioNovo,
            $"{RespostasPorLote} respostas em sequência, questões distintas.");
        PerformanceReport.EscreverSecao(
            $"Fluxo 3b — Sessão de estudos: usuário VETERANO ({HistoricoDoVeterano} respostas/revisões prévias)",
            temposUsuarioVeterano,
            $"Mesmo lote de {RespostasPorLote} respostas a questões novas, mesma máquina/execução — compare p50/p95 com 3a.");
    }
}
