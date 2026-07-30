using Forja.Application.Estudo;
using Forja.Application.Questoes;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Estudo;

/// <summary>
/// Teste de integração de <see cref="RegistrarRespostaComEfeitosService"/> contra um Postgres real
/// (Testcontainers) — o orquestrador criado para garantir que resposta e revisão espaçada só existem
/// juntas no banco (motivo do commit de "persistência unificada"). Mock não pega violação real de FK
/// nem comportamento de transação, então este teste roda com repositórios reais de ponta a ponta.
/// </summary>
[TestClass]
public class RegistrarRespostaComEfeitosServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    [TestMethod]
    public async Task RegistrarAsync_RespostaCorreta_PersisteRespostaRevisaoEPontuacaoNumaUnicaTransacao()
    {
        Guid usuarioId;
        Guid questaoId;
        using (var escopoSetup = CriarEscopo())
        {
            var contextSetup = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(contextSetup);
            var carreira = await CriarCarreiraAsync(contextSetup);
            var disciplina = await CriarDisciplinaAsync(contextSetup);
            var questao = new Questao
            {
                Id = Guid.NewGuid(),
                CarreiraId = carreira.Id,
                DisciplinaId = disciplina.Id,
                Tipo = TipoQuestao.CertoErrado,
                Enunciado = "Enunciado de teste",
                Gabarito = "certo",
                Explicacao = "Explicação de teste",
                Status = StatusQuestao.Aprovada,
                CriadoEm = DateTimeOffset.UtcNow,
            };
            contextSetup.Questoes.Add(questao);
            await contextSetup.SaveChangesAsync();
            usuarioId = usuario.Id;
            questaoId = questao.Id;
        }

        using (var escopoOperacao = CriarEscopo())
        {
            var questaoService = new QuestaoService(escopoOperacao.ServiceProvider.GetRequiredService<IQuestaoRepository>());
            var respostaService = new RespostaService(
                questaoService,
                escopoOperacao.ServiceProvider.GetRequiredService<IRespostaUsuarioRepository>(),
                escopoOperacao.ServiceProvider.GetRequiredService<IPontuacaoRepository>());
            var revisaoEspacadaService = new RevisaoEspacadaService(escopoOperacao.ServiceProvider.GetRequiredService<IRevisaoEspacadaRepository>());
            var orquestrador = new RegistrarRespostaComEfeitosService(
                respostaService,
                revisaoEspacadaService,
                escopoOperacao.ServiceProvider.GetRequiredService<IUnitOfWork>());

            var resultado = await orquestrador.RegistrarAsync(
                usuarioId, questaoId, respostaDada: "certo", tempoRespostaMs: 10_000, pomodoroId: null, ehRevisao: false);

            resultado.Resposta.Correta.Should().BeTrue();
            resultado.Resposta.Pontuada.Should().BeTrue();
        }

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var respostaPersistida = await contextLeitura.RespostasUsuario
            .AsNoTracking()
            .SingleAsync(r => r.UsuarioId == usuarioId && r.QuestaoId == questaoId);
        respostaPersistida.Correta.Should().BeTrue();
        respostaPersistida.Pontuada.Should().BeTrue();

        var revisaoPersistida = await contextLeitura.RevisoesEspacadas
            .AsNoTracking()
            .SingleAsync(r => r.UsuarioId == usuarioId && r.QuestaoId == questaoId);
        revisaoPersistida.ErrosConsecutivos.Should().Be(0);
        revisaoPersistida.IntervaloDiasAtual.Should().Be(2, "acerto dobra o intervalo inicial de 1 dia (RN-003)");

        var pontuacaoPersistida = await contextLeitura.Pontuacoes.FindAsync(usuarioId);
        pontuacaoPersistida.Should().NotBeNull();
        pontuacaoPersistida!.PontosTotal.Should().Be(10);
    }
}
