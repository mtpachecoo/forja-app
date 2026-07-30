using Forja.Application.Estudo;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Conteudo;
using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace Forja.Integration.Tests.Estudo;

/// <summary>
/// Teste de integração de <see cref="PlanoEstudoService"/> contra um Postgres real (Testcontainers) —
/// cobre exatamente o cenário que causava o bug original de ordem de FK: inserir um <see cref="PlanoEstudo"/>
/// e N <see cref="PlanoItem"/> na mesma chamada de <c>SaveChangesAsync</c>. Só <see cref="Forja.Application.Estudo.PesoDisciplinaService"/>
/// (dependência deste serviço) tinha ganhado cobertura de integração antes — o próprio fluxo de geração/
/// recriação de plano nunca tinha rodado contra banco real.
/// </summary>
[TestClass]
public class PlanoEstudoServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static PlanoEstudoService CriarServico(IServiceScope escopo, IChatCompletionService chatCompletionService) => new(
        escopo.ServiceProvider.GetRequiredService<IPlanoEstudoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IPlanoItemRepository>(),
        escopo.ServiceProvider.GetRequiredService<IEditalRepository>(),
        escopo.ServiceProvider.GetRequiredService<ITopicoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IDisciplinaRepository>(),
        new PesoDisciplinaService(
            escopo.ServiceProvider.GetRequiredService<IEditalPesoDisciplinaRepository>(),
            escopo.ServiceProvider.GetRequiredService<IEditalRepository>(),
            escopo.ServiceProvider.GetRequiredService<ITopicoRepository>(),
            escopo.ServiceProvider.GetRequiredService<IDisciplinaRepository>(),
            escopo.ServiceProvider.GetRequiredService<IChunkConteudoRepository>(),
            escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>(),
            Mock.Of<IChatCompletionService>(), // sem chunks/questões aprovadas cadastrados, cai no caso (d) — nunca chama o LLM.
            escopo.ServiceProvider.GetRequiredService<IUnitOfWork>()),
        chatCompletionService,
        escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

    private static Mock<IChatCompletionService> CriarMockDeAlocacao(params Guid[] topicoIds)
    {
        var itens = string.Join(",", topicoIds.Select(id => $$"""{"topicoId": "{{id}}", "tempoAlocadoMin": 30}"""));
        var json = $$"""{"itens": [{{itens}}]}""";

        var mock = new Mock<IChatCompletionService>();
        mock.Setup(c => c.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, json)]);
        return mock;
    }

    [TestMethod]
    public async Task ObterOuGerarPlanoAtualAsync_PrimeiraChamada_PersisteConfirmaFkENaoRecalculaDepois()
    {
        Guid usuarioId, carreiraId, topico1Id, topico2Id;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(context);
            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            var banca = new Banca { Id = Guid.NewGuid(), Nome = $"Banca Teste {Guid.NewGuid():N}", CriadoEm = DateTimeOffset.UtcNow };
            context.Bancas.Add(banca);
            var edital = new Edital { Id = Guid.NewGuid(), CarreiraId = carreira.Id, BancaId = banca.Id, Ano = 2024, CriadoEm = DateTimeOffset.UtcNow };
            context.Editais.Add(edital);
            var topico1 = new Topico { Id = Guid.NewGuid(), EditalId = edital.Id, DisciplinaId = disciplina.Id, Nome = "Tópico 1", Ordem = 1 };
            var topico2 = new Topico { Id = Guid.NewGuid(), EditalId = edital.Id, DisciplinaId = disciplina.Id, Nome = "Tópico 2", Ordem = 2 };
            context.Topicos.AddRange(topico1, topico2);
            await context.SaveChangesAsync();

            usuarioId = usuario.Id;
            carreiraId = carreira.Id;
            topico1Id = topico1.Id;
            topico2Id = topico2.Id;
        }

        PlanoGerado planoGerado;
        using (var escopoOperacao = CriarEscopo())
        {
            var servico = CriarServico(escopoOperacao, CriarMockDeAlocacao(topico1Id, topico2Id).Object);
            planoGerado = await servico.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, tempoDisponivelMinDia: 60, nivel: NivelUsuario.Intermediario);
        }

        planoGerado.Itens.Should().HaveCount(2);

        using (var escopoLeitura1 = CriarEscopo())
        {
            var contextLeitura = escopoLeitura1.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var planoPersistido = await contextLeitura.PlanosEstudo.FindAsync(planoGerado.Plano.Id);
            planoPersistido.Should().NotBeNull();
            planoPersistido!.UsuarioId.Should().Be(usuarioId);

            var itensPersistidos = await contextLeitura.PlanoItens
                .AsNoTracking()
                .Where(i => i.PlanoId == planoGerado.Plano.Id)
                .ToListAsync();
            itensPersistidos.Should().HaveCount(2);
            itensPersistidos.Select(i => i.TopicoId).Should().BeEquivalentTo([topico1Id, topico2Id]);
        }

        // Segunda chamada: não deve gerar um novo plano, deve reaproveitar o existente.
        using (var escopoSegundaChamada = CriarEscopo())
        {
            var servico = CriarServico(escopoSegundaChamada, CriarMockDeAlocacao(topico1Id, topico2Id).Object);
            var planoReaproveitado = await servico.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, tempoDisponivelMinDia: 60, nivel: NivelUsuario.Intermediario);
            planoReaproveitado.Plano.Id.Should().Be(planoGerado.Plano.Id);
        }

        using var escopoLeitura2 = CriarEscopo();
        var contextLeituraFinal = escopoLeitura2.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var totalDePlanos = await contextLeituraFinal.PlanosEstudo.CountAsync(p => p.UsuarioId == usuarioId);
        totalDePlanos.Should().Be(1, "a segunda chamada não deveria ter criado outro plano");
    }

    [TestMethod]
    public async Task RecriarPlanoAsync_QuandoJaExistePlano_MantemOAnteriorEGeraUmNovoComResumoCorreto()
    {
        Guid usuarioId, carreiraId, topicoId;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(context);
            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            var banca = new Banca { Id = Guid.NewGuid(), Nome = $"Banca Teste {Guid.NewGuid():N}", CriadoEm = DateTimeOffset.UtcNow };
            context.Bancas.Add(banca);
            var edital = new Edital { Id = Guid.NewGuid(), CarreiraId = carreira.Id, BancaId = banca.Id, Ano = 2024, CriadoEm = DateTimeOffset.UtcNow };
            context.Editais.Add(edital);
            var topico = new Topico { Id = Guid.NewGuid(), EditalId = edital.Id, DisciplinaId = disciplina.Id, Nome = "Tópico único", Ordem = 1 };
            context.Topicos.Add(topico);
            await context.SaveChangesAsync();

            usuarioId = usuario.Id;
            carreiraId = carreira.Id;
            topicoId = topico.Id;
        }

        Guid planoAnteriorId;
        using (var escopoPrimeiraChamada = CriarEscopo())
        {
            var servico = CriarServico(escopoPrimeiraChamada, CriarMockDeAlocacao(topicoId).Object);
            var planoInicial = await servico.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, tempoDisponivelMinDia: 60, nivel: NivelUsuario.Intermediario);
            planoAnteriorId = planoInicial.Plano.Id;
        }

        RecriarPlanoResultado resultado;
        using (var escopoRecriar = CriarEscopo())
        {
            var servico = CriarServico(escopoRecriar, CriarMockDeAlocacao(topicoId).Object);
            resultado = await servico.RecriarPlanoAsync(usuarioId, carreiraId, tempoDisponivelMinDia: 60, nivel: NivelUsuario.Intermediario);
        }

        resultado.ResumoAnterior.Should().NotBeNull();
        resultado.ResumoAnterior!.TotalTopicos.Should().Be(1);
        resultado.ResumoAnterior.TopicosConcluidos.Should().Be(0);
        resultado.PlanoNovo.Plano.Id.Should().NotBe(planoAnteriorId);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();

        // O plano anterior continua no banco como histórico — recriar não apaga, só deixa de ser o mais recente.
        var planoAnteriorAindaExiste = await contextLeitura.PlanosEstudo.FindAsync(planoAnteriorId);
        planoAnteriorAindaExiste.Should().NotBeNull();

        var totalDePlanos = await contextLeitura.PlanosEstudo.CountAsync(p => p.UsuarioId == usuarioId);
        totalDePlanos.Should().Be(2);
    }
}
