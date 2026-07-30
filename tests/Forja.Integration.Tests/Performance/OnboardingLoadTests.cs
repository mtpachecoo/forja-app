using System.Diagnostics;
using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Application.Onboarding;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Usuarios;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Forja.Integration.Tests.Performance;

/// <summary>
/// Fluxo 1 da auditoria de performance: N cadastros novos em sequência via
/// <see cref="OnboardingService.CompletarOnboardingAsync"/> (equivalente de <c>POST /onboarding</c>),
/// medindo p50/p95/p99 e confirmando persistência de usuário + plano + itens. IA mockada (resposta
/// instantânea e determinística) — latência de LLM é dependência externa, fora do controle deste
/// código; o que se mede aqui é o overhead do próprio backend (DB + orquestração).
/// </summary>
[TestClass]
public class OnboardingLoadTests : IntegrationTestBase
{
    private const int QuantidadeCadastros = 40;

    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static OnboardingService CriarServico(IServiceScope escopo, IGeradorDeRespostaChat geradorDeRespostaChat)
    {
        var planoEstudoService = new PlanoEstudoService(
            escopo.ServiceProvider.GetRequiredService<IPlanoEstudoRepository>(),
            escopo.ServiceProvider.GetRequiredService<IPlanoItemRepository>(),
            escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.IEditalRepository>(),
            escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.ITopicoRepository>(),
            escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.IDisciplinaRepository>(),
            new PesoDisciplinaService(
                escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.IEditalPesoDisciplinaRepository>(),
                escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.IEditalRepository>(),
                escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.ITopicoRepository>(),
                escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.IDisciplinaRepository>(),
                escopo.ServiceProvider.GetRequiredService<Forja.Domain.Conteudo.IChunkConteudoRepository>(),
                escopo.ServiceProvider.GetRequiredService<Forja.Domain.Questoes.IQuestaoRepository>(),
                Mock.Of<IGeradorDeRespostaChat>(), // sem chunks/questões aprovadas cadastrados, nunca chama o LLM.
                escopo.ServiceProvider.GetRequiredService<IUnitOfWork>()),
            geradorDeRespostaChat,
            escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

        return new OnboardingService(
            escopo.ServiceProvider.GetRequiredService<IUsuarioRepository>(),
            planoEstudoService,
            escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    private static Mock<IGeradorDeRespostaChat> CriarMockDeAlocacao(IReadOnlyList<Guid> topicoIds)
    {
        var itens = string.Join(",", topicoIds.Select(id => $$"""{"topicoId": "{{id}}", "tempoAlocadoMin": 30}"""));
        var json = $$"""{"itens": [{{itens}}]}""";

        var mock = new Mock<IGeradorDeRespostaChat>();
        mock.Setup(c => c.GerarRespostaAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);
        return mock;
    }

    [TestMethod]
    public async Task Carga_NCadastrosEmSequencia_MedeTemposEConfirmaPersistencia()
    {
        Guid carreiraId, disciplinaId;
        List<Guid> topicoIds;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            var (_, topicos) = await PerformanceFixtures.CriarEditalComTopicosAsync(context, carreira.Id, disciplina.Id, quantidadeTopicos: 5);

            carreiraId = carreira.Id;
            disciplinaId = disciplina.Id;
            topicoIds = topicos.Select(t => t.Id).ToList();
        }

        var geradorDeRespostaChat = CriarMockDeAlocacao(topicoIds).Object;
        var tempos = new List<double>();
        var usuarioIds = new List<Guid>();
        var cronometro = new Stopwatch();

        for (var i = 0; i < QuantidadeCadastros; i++)
        {
            Guid usuarioId;
            using (var escopoCriacao = CriarEscopo())
            {
                var context = escopoCriacao.ServiceProvider.GetRequiredService<ForjaDbContext>();
                var usuario = await CriarUsuarioAsync(context);
                usuarioId = usuario.Id;
            }

            using var escopoOperacao = CriarEscopo();
            var servico = CriarServico(escopoOperacao, geradorDeRespostaChat);

            cronometro.Restart();
            await servico.CompletarOnboardingAsync(usuarioId, carreiraId, tempoDisponivelMinDia: 60 + i, nivel: NivelUsuario.Intermediario);
            cronometro.Stop();

            tempos.Add(cronometro.Elapsed.TotalMilliseconds);
            usuarioIds.Add(usuarioId);
        }

        PerformanceReport.EscreverSecao(
            "Fluxo 1 — Onboarding (POST /onboarding, N cadastros em sequência)",
            tempos,
            $"IA mockada (resposta instantânea) — mede overhead de DB/orquestração, não latência de LLM real. N={QuantidadeCadastros}.");

        // Confirma persistência correta (não só que não lançou): usuário + plano + itens de uma amostra.
        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var totalUsuariosComTempoAtualizado = await contextLeitura.Usuarios
            .CountAsync(u => usuarioIds.Contains(u.Id) && u.TempoDisponivelMinDia >= 60);
        totalUsuariosComTempoAtualizado.Should().Be(QuantidadeCadastros);

        var totalPlanos = await contextLeitura.PlanosEstudo.CountAsync(p => usuarioIds.Contains(p.UsuarioId));
        totalPlanos.Should().Be(QuantidadeCadastros, "cada onboarding deveria gerar exatamente um plano");

        var planoAmostra = await contextLeitura.PlanosEstudo.FirstAsync(p => p.UsuarioId == usuarioIds[0]);
        var itensDoAmostra = await contextLeitura.PlanoItens.CountAsync(i => i.PlanoId == planoAmostra.Id);
        itensDoAmostra.Should().Be(topicoIds.Count, "o plano deveria ter um item por tópico alocado");
    }
}
