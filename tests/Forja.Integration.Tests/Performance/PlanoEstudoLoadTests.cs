using System.Diagnostics;
using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Usuarios;
using Forja.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Forja.Integration.Tests.Performance;

/// <summary>
/// Fluxo 2 da auditoria de performance: <c>GET /plano/atual</c>
/// (<see cref="IPlanoEstudoService.ObterOuGerarPlanoAtualAsync"/>), medindo separadamente a primeira
/// geração (chama IA, grava plano+itens) da leitura de um plano já existente (só consulta) — misturar
/// os dois esconderia uma regressão real num dos dois caminhos.
/// </summary>
[TestClass]
public class PlanoEstudoLoadTests : IntegrationTestBase
{
    private const int QuantidadeUsuarios = 40;

    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static PlanoEstudoService CriarServico(IServiceScope escopo, IGeradorDeRespostaChat geradorDeRespostaChat) => new(
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
            Mock.Of<IGeradorDeRespostaChat>(),
            escopo.ServiceProvider.GetRequiredService<IUnitOfWork>()),
        geradorDeRespostaChat,
        escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

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
    public async Task Carga_PrimeiraGeracaoVsLeituraDePlanoExistente_MedeSeparadamente()
    {
        Guid carreiraId, disciplinaId;
        List<Guid> topicoIds;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            var (_, topicos) = await PerformanceFixtures.CriarEditalComTopicosAsync(context, carreira.Id, disciplina.Id, quantidadeTopicos: 8);

            carreiraId = carreira.Id;
            disciplinaId = disciplina.Id;
            topicoIds = topicos.Select(t => t.Id).ToList();
        }

        var geradorDeRespostaChat = CriarMockDeAlocacao(topicoIds).Object;
        var temposGeracao = new List<double>();
        var temposLeitura = new List<double>();
        var cronometro = new Stopwatch();

        for (var i = 0; i < QuantidadeUsuarios; i++)
        {
            Guid usuarioId;
            using (var escopoCriacao = CriarEscopo())
            {
                var context = escopoCriacao.ServiceProvider.GetRequiredService<ForjaDbContext>();
                var usuario = await CriarUsuarioAsync(context);
                usuarioId = usuario.Id;
            }

            using (var escopoGeracao = CriarEscopo())
            {
                var servico = CriarServico(escopoGeracao, geradorDeRespostaChat);
                cronometro.Restart();
                await servico.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, tempoDisponivelMinDia: 60, nivel: NivelUsuario.Intermediario);
                cronometro.Stop();
                temposGeracao.Add(cronometro.Elapsed.TotalMilliseconds);
            }

            using (var escopoLeitura = CriarEscopo())
            {
                var servico = CriarServico(escopoLeitura, geradorDeRespostaChat);
                cronometro.Restart();
                await servico.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, tempoDisponivelMinDia: 60, nivel: NivelUsuario.Intermediario);
                cronometro.Stop();
                temposLeitura.Add(cronometro.Elapsed.TotalMilliseconds);
            }
        }

        PerformanceReport.EscreverSecao(
            "Fluxo 2a — Plano de estudos: PRIMEIRA GERAÇÃO (chama IA, grava plano+itens)",
            temposGeracao,
            $"IA mockada (resposta instantânea) — mede overhead de DB/orquestração próprio. N={QuantidadeUsuarios}.");
        PerformanceReport.EscreverSecao(
            "Fluxo 2b — Plano de estudos: LEITURA de plano já existente (só query, sem IA)",
            temposLeitura,
            $"Mesmos {QuantidadeUsuarios} usuários, segunda chamada imediatamente após a primeira.");
    }
}
