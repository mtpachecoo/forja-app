using Forja.Application.Estudo;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Estudo;

/// <summary>
/// Testes de integração de <see cref="PomodoroService"/> contra um Postgres real (Testcontainers),
/// cobrindo o fluxo que nunca tinha rodado contra dado real: inserir e ler <c>pomodoros</c> e a
/// concessão de pontos (RN-011) na tabela <c>pontuacoes</c>.
/// </summary>
[TestClass]
public class PomodoroServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static PomodoroService CriarServico(IServiceScope escopo) => new(
        escopo.ServiceProvider.GetRequiredService<ISessaoEstudoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IPomodoroRepository>(),
        escopo.ServiceProvider.GetRequiredService<IRespostaUsuarioRepository>(),
        escopo.ServiceProvider.GetRequiredService<IPontuacaoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

    [TestMethod]
    public async Task IniciarPomodoroAsync_PersisteNoBancoComFkParaSessao()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var usuario = await CriarUsuarioAsync(context);
        var sessao = new SessaoEstudo { Id = Guid.NewGuid(), UsuarioId = usuario.Id, DataSessao = DateOnly.FromDateTime(DateTime.UtcNow), CriadoEm = DateTimeOffset.UtcNow };
        context.SessoesEstudo.Add(sessao);
        await context.SaveChangesAsync();

        var servico = CriarServico(escopo);
        var pomodoro = await servico.IniciarPomodoroAsync(usuario.Id, sessao.Id);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var persistido = await contextLeitura.Pomodoros.FindAsync(pomodoro.Id);

        persistido.Should().NotBeNull();
        persistido!.SessaoId.Should().Be(sessao.Id);
        persistido.FinalizadoEm.Should().BeNull();
    }

    [TestMethod]
    public async Task FinalizarPomodoroAsync_SemRespostas_NaoConcedePontos()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var usuario = await CriarUsuarioAsync(context);
        var sessao = new SessaoEstudo { Id = Guid.NewGuid(), UsuarioId = usuario.Id, DataSessao = DateOnly.FromDateTime(DateTime.UtcNow), CriadoEm = DateTimeOffset.UtcNow };
        context.SessoesEstudo.Add(sessao);
        await context.SaveChangesAsync();

        var servico = CriarServico(escopo);
        var pomodoro = await servico.IniciarPomodoroAsync(usuario.Id, sessao.Id);

        var resultado = await servico.FinalizarPomodoroAsync(usuario.Id, sessao.Id, pomodoro.Id);

        resultado.Pontuacao.Should().BeNull();
        resultado.Pomodoro.QtdRespostasNoCiclo.Should().Be(0);
        resultado.Pomodoro.PontosConcedidos.Should().Be(0);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        (await contextLeitura.Pontuacoes.FindAsync(usuario.Id)).Should().BeNull();
    }

    [TestMethod]
    public async Task FinalizarPomodoroAsync_ComRespostaRegistrada_ConcedePontosEPersisteFks()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var usuario = await CriarUsuarioAsync(context);
        var carreira = await CriarCarreiraAsync(context);
        var disciplina = await CriarDisciplinaAsync(context);
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
        context.Questoes.Add(questao);
        var sessao = new SessaoEstudo { Id = Guid.NewGuid(), UsuarioId = usuario.Id, DataSessao = DateOnly.FromDateTime(DateTime.UtcNow), CriadoEm = DateTimeOffset.UtcNow };
        context.SessoesEstudo.Add(sessao);
        await context.SaveChangesAsync();

        var servico = CriarServico(escopo);
        var pomodoro = await servico.IniciarPomodoroAsync(usuario.Id, sessao.Id);

        // Resposta registrada durante o ciclo (via repositório real, com FK pra pomodoro/questao/usuario).
        context.RespostasUsuario.Add(new RespostaUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            QuestaoId = questao.Id,
            PomodoroId = pomodoro.Id,
            RespostaDada = "certo",
            Correta = true,
            TempoRespostaMs = 1000,
            CriadoEm = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var resultado = await servico.FinalizarPomodoroAsync(usuario.Id, sessao.Id, pomodoro.Id);

        resultado.Pontuacao.Should().NotBeNull();
        resultado.Pontuacao!.PontosTotal.Should().Be(5);
        resultado.Pomodoro.QtdRespostasNoCiclo.Should().Be(1);
        resultado.Pomodoro.PontosConcedidos.Should().Be(5);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var pontuacaoPersistida = await contextLeitura.Pontuacoes.FindAsync(usuario.Id);
        pontuacaoPersistida.Should().NotBeNull();
        pontuacaoPersistida!.PontosTotal.Should().Be(5);

        var pomodoroPersistido = await contextLeitura.Pomodoros.FindAsync(pomodoro.Id);
        pomodoroPersistido!.FinalizadoEm.Should().NotBeNull();
    }
}
