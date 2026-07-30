using Forja.Application.Estudo;
using Forja.Application.Questoes;
using Forja.Domain.Common;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Performance;

/// <summary>
/// Fluxo 4 (o mais importante) da auditoria de performance: teste de condição de corrida em
/// <see cref="Forja.Domain.Gamificacao.Pontuacao"/>. Antes da correção, <see cref="RespostaService"/>
/// lê a pontuação via <c>GetByIdAsync</c> (AsNoTracking), muta em memória
/// (<see cref="Forja.Domain.Gamificacao.Pontuacao.RegistrarPontos"/>) e salva via <c>Update</c>/
/// <c>AddAsync</c> — sem token de concorrência (rowversion/xmin). Dois cenários, duas manifestações
/// diferentes da mesma causa raiz:
/// <list type="bullet">
/// <item>Usuário sem <c>Pontuacao</c> prévia: N respostas concorrentes competem pelo caminho de
/// INSERT — Postgres rejeita as duplicatas de <c>usuario_id</c> com <c>23505</c>, quebrando a
/// requisição (mais visível que perda silenciosa, mas ainda um bug real de concorrência).</item>
/// <item>Usuário já com <c>Pontuacao</c> existente: N respostas concorrentes competem pelo caminho de
/// UPDATE — o clássico "lost update" descrito no pedido: cada uma lê o valor antigo, soma por conta
/// própria, e a que salva por último sobrescreve a outra, silenciosamente.</item>
/// </list>
/// </summary>
[TestClass]
public class PontuacaoConcorrenciaTests : IntegrationTestBase
{
    private const int RespostasConcorrentes = 20;
    private const int PontosPorAcerto = 10;

    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static RegistrarRespostaComEfeitosService CriarServico(IServiceScope escopo) => new(
        new RespostaService(
            new QuestaoService(escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>()),
            escopo.ServiceProvider.GetRequiredService<Forja.Domain.Estudo.IRespostaUsuarioRepository>(),
            escopo.ServiceProvider.GetRequiredService<Forja.Domain.Gamificacao.IPontuacaoRepository>()),
        new RevisaoEspacadaService(escopo.ServiceProvider.GetRequiredService<Forja.Domain.Estudo.IRevisaoEspacadaRepository>()),
        escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

    /// <summary>
    /// Responde <paramref name="questoes"/> concorrentemente (um DbContext por chamada, igual N
    /// requisições HTTP reais), sem deixar uma exceção individual derrubar as demais — devolve
    /// separadamente quantas tiveram sucesso e as exceções das que falharam.
    /// </summary>
    private static async Task<(int Sucessos, List<Exception> Falhas)> ResponderConcorrentementeAsync(Guid usuarioId, IReadOnlyList<Questao> questoes)
    {
        var tarefas = questoes.Select(async questao =>
        {
            using var escopo = CriarEscopo();
            var servico = CriarServico(escopo);
            await servico.RegistrarAsync(usuarioId, questao.Id, "Certo", tempoRespostaMs: 8_000, pomodoroId: null, ehRevisao: false);
        }).ToList();

        var resultados = await Task.WhenAll(tarefas.Select(async t =>
        {
            try
            {
                await t;
                return (Sucesso: true, Erro: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Sucesso: false, Erro: ex);
            }
        }));

        return (resultados.Count(r => r.Sucesso), resultados.Where(r => r.Erro is not null).Select(r => r.Erro!).ToList());
    }

    [TestMethod]
    public async Task RespostasConcorrentes_UsuarioSemPontuacaoPrevia_ExpoeCorridaNoInsert()
    {
        Guid usuarioId;
        List<Questao> questoes;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            var usuario = await CriarUsuarioAsync(context);
            usuarioId = usuario.Id;
            questoes = await PerformanceFixtures.CriarQuestoesAprovadasAsync(context, carreira.Id, disciplina.Id, RespostasConcorrentes);
        }

        var (sucessos, falhas) = await ResponderConcorrentementeAsync(usuarioId, questoes);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var pontuacaoFinal = await contextLeitura.Pontuacoes.AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == usuarioId);

        var tiposDeErro = falhas.Select(f => f.InnerException?.GetType().Name ?? f.GetType().Name).Distinct().ToList();

        PerformanceReport.EscreverLinha(
            $"## Fluxo 4a — Concorrência em Pontuacao: usuário SEM Pontuacao prévia (corrida no INSERT), {RespostasConcorrentes} respostas concorrentes\n\n"
            + $"- Respostas com sucesso: {sucessos}/{RespostasConcorrentes}\n"
            + $"- Respostas que lançaram exceção: {falhas.Count} (tipos: {string.Join(", ", tiposDeErro)})\n"
            + $"- PontosTotal final: {(pontuacaoFinal?.PontosTotal.ToString() ?? "null (nenhuma linha gravada)")}\n"
            + $"- Esperado se não houvesse corrida: {RespostasConcorrentes * PontosPorAcerto}");

        falhas.Should().BeEmpty(
            "nenhuma resposta individualmente válida deveria falhar por causa de outra resposta concorrente do mesmo usuário — " +
            "uma corrida no INSERT de Pontuacao não deveria derrubar a requisição");
    }

    [TestMethod]
    public async Task RespostasConcorrentes_UsuarioComPontuacaoExistente_PontosTotalDeveSomarExato_SemPerderIncremento()
    {
        Guid usuarioId;
        List<Questao> questoes;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            var usuario = await CriarUsuarioAsync(context);
            usuarioId = usuario.Id;
            // Questões distintas: cada resposta pontua de verdade (RN-008 só pontua a primeira resposta
            // correta por questão) — o teste precisa que TODAS as N respostas sejam pontuáveis ao mesmo
            // tempo pra expor a corrida; reusar a mesma questão mascararia o problema.
            questoes = await PerformanceFixtures.CriarQuestoesAprovadasAsync(context, carreira.Id, disciplina.Id, RespostasConcorrentes + 1);
        }

        // "Priming": uma resposta síncrona primeiro, só pra garantir que a linha de Pontuacao já existe
        // antes da rajada concorrente — isola o cenário de UPDATE (lost update) do cenário de INSERT
        // (coberto no outro teste desta classe), evitando misturar as duas causas na mesma amostra.
        using (var escopoPriming = CriarEscopo())
        {
            var servico = CriarServico(escopoPriming);
            await servico.RegistrarAsync(usuarioId, questoes[0].Id, "Certo", tempoRespostaMs: 8_000, pomodoroId: null, ehRevisao: false);
        }

        var questoesConcorrentes = questoes.Skip(1).ToList();
        var (sucessos, falhas) = await ResponderConcorrentementeAsync(usuarioId, questoesConcorrentes);
        falhas.Should().BeEmpty("com a linha de Pontuacao já existente, nenhuma resposta deveria lançar exceção — só perder incremento silenciosamente");

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var pontuacaoFinal = await contextLeitura.Pontuacoes.AsNoTracking().SingleAsync(p => p.UsuarioId == usuarioId);

        var esperado = (RespostasConcorrentes + 1) * PontosPorAcerto;
        var respostasPontuadasPersistidas = await contextLeitura.RespostasUsuario
            .CountAsync(r => r.UsuarioId == usuarioId && r.Pontuada);

        PerformanceReport.EscreverLinha(
            $"## Fluxo 4b — Concorrência em Pontuacao: usuário COM Pontuacao existente (UPDATE, lost update), {RespostasConcorrentes} respostas concorrentes + 1 de priming\n\n"
            + $"- Esperado: {esperado} pontos ({RespostasConcorrentes + 1} respostas x {PontosPorAcerto})\n"
            + $"- PontosTotal obtido: {pontuacaoFinal.PontosTotal}\n"
            + $"- Respostas persistidas com Pontuada=true: {respostasPontuadasPersistidas}/{RespostasConcorrentes + 1}\n"
            + $"- Perdidos: {esperado - pontuacaoFinal.PontosTotal} pontos ({(esperado - pontuacaoFinal.PontosTotal) / (double)PontosPorAcerto:F0} incrementos)"
            + (respostasPontuadasPersistidas == RespostasConcorrentes + 1 && pontuacaoFinal.PontosTotal < esperado
                ? "\n- **LOST UPDATE CONFIRMADO**: todas as respostas foram marcadas como pontuadas individualmente (RespostaUsuario.Pontuada=true), mas Pontuacao.PontosTotal ficou menor que a soma — incrementos concorrentes se perderam entre leitura e salvamento."
                : ""));

        pontuacaoFinal.PontosTotal.Should().Be(esperado,
            "cada uma das respostas concorrentes é individualmente correta e pontuável — nenhum incremento deveria se perder");
    }
}
