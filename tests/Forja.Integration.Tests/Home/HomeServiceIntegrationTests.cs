using Forja.Application.Home;
using Forja.Domain.Catalogo;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Home;

/// <summary>
/// Teste de integração de <see cref="HomeService"/> contra um Postgres real (Testcontainers) — seed de
/// streak, pontuação, plano parcialmente concluído e respostas, validando a resposta agregada completa.
/// </summary>
[TestClass]
public class HomeServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static HomeService CriarServico(IServiceScope escopo) => new(
        escopo.ServiceProvider.GetRequiredService<IStreakRepository>(),
        escopo.ServiceProvider.GetRequiredService<IPontuacaoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IPlanoEstudoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IPlanoItemRepository>(),
        escopo.ServiceProvider.GetRequiredService<IRespostaUsuarioRepository>());

    [TestMethod]
    public async Task ObterResumoAsync_ComStreakPontuacaoPlanoERespostasReais_AgregaTudoCorretamente()
    {
        Guid usuarioId;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(context);
            usuarioId = usuario.Id;

            context.Streaks.Add(new Streak { UsuarioId = usuarioId, DiasConsecutivos = 5, UltimaAtividadeEm = DateOnly.FromDateTime(DateTime.UtcNow) });
            context.Pontuacoes.Add(new Pontuacao
            {
                UsuarioId = usuarioId,
                PontosTotal = 300,
                PontosSemanaAtual = 20,
                SemanaReferencia = Pontuacao.InicioDaSemana(DateOnly.FromDateTime(DateTime.UtcNow)),
            });

            var carreira = await CriarCarreiraAsync(context);
            var disciplina = await CriarDisciplinaAsync(context);
            var banca = new Banca { Id = Guid.NewGuid(), Nome = $"Banca Teste {Guid.NewGuid():N}", CriadoEm = DateTimeOffset.UtcNow };
            context.Bancas.Add(banca);
            var edital = new Edital { Id = Guid.NewGuid(), CarreiraId = carreira.Id, BancaId = banca.Id, Ano = 2024, CriadoEm = DateTimeOffset.UtcNow };
            context.Editais.Add(edital);
            var topico = new Topico { Id = Guid.NewGuid(), EditalId = edital.Id, DisciplinaId = disciplina.Id, Nome = "Tópico", Ordem = 1 };
            context.Topicos.Add(topico);
            await context.SaveChangesAsync();

            var plano = new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, CarreiraId = carreira.Id, CriadoEm = DateTimeOffset.UtcNow };
            context.PlanosEstudo.Add(plano);
            await context.SaveChangesAsync();

            context.PlanoItens.AddRange(
                new PlanoItem { Id = Guid.NewGuid(), PlanoId = plano.Id, TopicoId = topico.Id, Ordem = 1, TempoAlocadoMin = 30, Status = StatusItemPlano.Concluido },
                new PlanoItem { Id = Guid.NewGuid(), PlanoId = plano.Id, TopicoId = topico.Id, Ordem = 2, TempoAlocadoMin = 30, Status = StatusItemPlano.Pendente });
            await context.SaveChangesAsync();

            var questao = new Questao
            {
                Id = Guid.NewGuid(),
                CarreiraId = carreira.Id,
                DisciplinaId = disciplina.Id,
                Tipo = TipoQuestao.CertoErrado,
                Enunciado = "Enunciado",
                Gabarito = "Certo",
                Explicacao = "Explicação",
                CriadoEm = DateTimeOffset.UtcNow,
            };
            context.Questoes.Add(questao);
            await context.SaveChangesAsync();

            context.RespostasUsuario.Add(new RespostaUsuario
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                QuestaoId = questao.Id,
                RespostaDada = "Certo",
                Correta = true,
                TempoRespostaMs = 1000,
                CriadoEm = DateTimeOffset.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        using var escopoOperacao = CriarEscopo();
        var servico = CriarServico(escopoOperacao);
        var resumo = await servico.ObterResumoAsync(usuarioId, 10);

        resumo.DiasConsecutivos.Should().Be(5);
        resumo.PontosTotal.Should().Be(300);
        resumo.PontosSemanaAtual.Should().Be(20);
        resumo.PercentualPlanoConcluido.Should().Be(50m);
        resumo.RespostasHoje.Should().Be(1);
        resumo.AtividadesRecentes.Should().ContainSingle(a => a.Correta);
    }
}
