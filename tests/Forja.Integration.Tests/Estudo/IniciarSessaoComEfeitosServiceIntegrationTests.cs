using Forja.Application.Estudo;
using Forja.Application.Gamificacao;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Estudo;

/// <summary>
/// Teste de integração de <see cref="IniciarSessaoComEfeitosService"/> contra um Postgres real
/// (Testcontainers) — o orquestrador que garante que sessão de estudo e streak são persistidos numa
/// única transação. Mock não pega violação real de FK nem comportamento de transação, então este teste
/// roda com repositórios reais de ponta a ponta.
/// </summary>
[TestClass]
public class IniciarSessaoComEfeitosServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static IniciarSessaoComEfeitosService CriarServico(IServiceScope escopo)
    {
        var sessaoEstudoService = new SessaoEstudoService(escopo.ServiceProvider.GetRequiredService<ISessaoEstudoRepository>());
        var streakService = new StreakService(escopo.ServiceProvider.GetRequiredService<IStreakRepository>());
        return new IniciarSessaoComEfeitosService(sessaoEstudoService, streakService, escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    [TestMethod]
    public async Task IniciarAsync_PrimeiraSessaoDoUsuario_PersisteSessaoENovoStreakNumaUnicaTransacao()
    {
        Guid usuarioId;
        using (var escopoSetup = CriarEscopo())
        {
            var contextSetup = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(contextSetup);
            usuarioId = usuario.Id;
        }

        SessaoEstudo sessao;
        using (var escopoOperacao = CriarEscopo())
        {
            sessao = await CriarServico(escopoOperacao).IniciarAsync(usuarioId);
        }

        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var sessaoPersistida = await contextLeitura.SessoesEstudo.FindAsync(sessao.Id);
        sessaoPersistida.Should().NotBeNull();
        sessaoPersistida!.UsuarioId.Should().Be(usuarioId);
        sessaoPersistida.DataSessao.Should().Be(hoje);

        var streakPersistido = await contextLeitura.Streaks.FindAsync(usuarioId);
        streakPersistido.Should().NotBeNull();
        streakPersistido!.DiasConsecutivos.Should().Be(1);
        streakPersistido.UltimaAtividadeEm.Should().Be(hoje);
    }

    [TestMethod]
    public async Task IniciarAsync_ChamadoDeNovoNoMesmoDia_ReaproveitaSessaoENaoDuplicaStreak()
    {
        Guid usuarioId;
        using (var escopoSetup = CriarEscopo())
        {
            var contextSetup = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(contextSetup);
            usuarioId = usuario.Id;
        }

        Guid primeiraSessaoId;
        using (var escopoPrimeiraChamada = CriarEscopo())
        {
            primeiraSessaoId = (await CriarServico(escopoPrimeiraChamada).IniciarAsync(usuarioId)).Id;
        }

        SessaoEstudo segundaSessao;
        using (var escopoSegundaChamada = CriarEscopo())
        {
            segundaSessao = await CriarServico(escopoSegundaChamada).IniciarAsync(usuarioId);
        }

        segundaSessao.Id.Should().Be(primeiraSessaoId, "no mesmo dia, a sessão existente deve ser reaproveitada, não duplicada");

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var totalDeSessoes = await contextLeitura.SessoesEstudo.CountAsync(s => s.UsuarioId == usuarioId);
        totalDeSessoes.Should().Be(1);

        var streakPersistido = await contextLeitura.Streaks.FindAsync(usuarioId);
        streakPersistido!.DiasConsecutivos.Should().Be(1, "a segunda chamada no mesmo dia não deve incrementar o streak de novo");
    }
}
