using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Application.Onboarding;
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
using Moq;

namespace Forja.Integration.Tests.Onboarding;

/// <summary>
/// Teste de integração de <see cref="OnboardingService"/> contra um Postgres real (Testcontainers) —
/// cobre o fluxo completo do RF-002: usuário sem contexto prévio, onboarding atualiza <see cref="Usuario"/>
/// e gera o plano de estudo usando o contexto recém-informado (não os valores default de provisionamento).
/// </summary>
[TestClass]
public class OnboardingServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static OnboardingService CriarServico(IServiceScope escopo, IGeradorDeRespostaChat geradorDeRespostaChat)
    {
        var planoEstudoService = new PlanoEstudoService(
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
                Mock.Of<IGeradorDeRespostaChat>(), // sem chunks/questões aprovadas cadastrados, nunca chama o LLM.
                escopo.ServiceProvider.GetRequiredService<IUnitOfWork>()),
            geradorDeRespostaChat,
            escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

        return new OnboardingService(
            escopo.ServiceProvider.GetRequiredService<IUsuarioRepository>(),
            planoEstudoService,
            escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    private static Mock<IGeradorDeRespostaChat> CriarMockDeAlocacao(params Guid[] topicoIds)
    {
        var itens = string.Join(",", topicoIds.Select(id => $$"""{"topicoId": "{{id}}", "tempoAlocadoMin": 30}"""));
        var json = $$"""{"itens": [{{itens}}]}""";

        var mock = new Mock<IGeradorDeRespostaChat>();
        mock.Setup(c => c.GerarRespostaAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);
        return mock;
    }

    [TestMethod]
    public async Task CompletarOnboardingAsync_UsuarioSemDadosPrevios_AtualizaUsuarioEGeraPlanoComOContextoInformado()
    {
        Guid usuarioId, carreiraId, topicoId;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            // Usuário "recém-provisionado": Nivel/TempoDisponivelMinDia nos valores default do
            // primeiro acesso (ver UsuarioService.ResolverUsuarioAutenticadoAsync) — exatamente o
            // estado de quem nunca passou pelo onboarding.
            var usuario = await CriarUsuarioAsync(context);
            usuario.Nivel = NivelUsuario.Iniciante;
            usuario.TempoDisponivelMinDia = 60;
            await context.SaveChangesAsync();

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

        OnboardingResultado resultado;
        using (var escopoOperacao = CriarEscopo())
        {
            var servico = CriarServico(escopoOperacao, CriarMockDeAlocacao(topicoId).Object);
            resultado = await servico.CompletarOnboardingAsync(usuarioId, carreiraId, 150, NivelUsuario.Avancado);
        }

        resultado.Usuario.TempoDisponivelMinDia.Should().Be(150);
        resultado.Usuario.Nivel.Should().Be(NivelUsuario.Avancado);
        resultado.Plano.Itens.Should().ContainSingle(i => i.TopicoId == topicoId);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var usuarioPersistido = await contextLeitura.Usuarios.AsNoTracking().FirstAsync(u => u.Id == usuarioId);
        usuarioPersistido.TempoDisponivelMinDia.Should().Be(150, "onboarding deve persistir o contexto informado, não o default de provisionamento");
        usuarioPersistido.Nivel.Should().Be(NivelUsuario.Avancado);

        var planoPersistido = await contextLeitura.PlanosEstudo.AsNoTracking().FirstAsync(p => p.UsuarioId == usuarioId);
        planoPersistido.CarreiraId.Should().Be(carreiraId);

        var itemPersistido = await contextLeitura.PlanoItens.AsNoTracking().FirstAsync(i => i.PlanoId == planoPersistido.Id);
        // Tempo alocado é limitado ao tempo disponível informado no onboarding (150), não ao default (60).
        itemPersistido.TempoAlocadoMin.Should().BeLessOrEqualTo(150);
    }
}
