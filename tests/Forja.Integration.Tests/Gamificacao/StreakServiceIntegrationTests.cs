using Forja.Application.Gamificacao;
using Forja.Domain.Common;
using Forja.Domain.Gamificacao;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Gamificacao;

/// <summary>
/// Testes de integração de <see cref="StreakService"/> contra um Postgres real (Testcontainers).
/// <see cref="StreakService.RegistrarAtividadeAsync"/> usa a data real do sistema como "hoje" (não é
/// injetável), então os cenários de incremento e reset são simulados controlando a
/// <see cref="Streak.UltimaAtividadeEm"/> já persistida, em vez de mockar o relógio. O streak inicial é
/// semeado num escopo (e portanto <see cref="ForjaDbContext"/>) diferente do usado para exercitar o
/// serviço, como duas requisições HTTP reais seriam — caso contrário a instância recém-semeada
/// continuaria rastreada pelo change tracker do mesmo contexto e colidiria com a leitura sem tracking
/// feita pelo repositório.
/// </summary>
[TestClass]
public class StreakServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    [TestMethod]
    public async Task RegistrarAtividadeAsync_UltimaAtividadeFoiOntem_IncrementaEPersisteNoBanco()
    {
        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        Guid usuarioId;
        using (var escopoSetup = CriarEscopo())
        {
            var contextSetup = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(contextSetup);
            contextSetup.Streaks.Add(new Streak { UsuarioId = usuario.Id, DiasConsecutivos = 4, UltimaAtividadeEm = hoje.AddDays(-1) });
            await contextSetup.SaveChangesAsync();
            usuarioId = usuario.Id;
        }

        using (var escopoOperacao = CriarEscopo())
        {
            var servico = new StreakService(escopoOperacao.ServiceProvider.GetRequiredService<IStreakRepository>());
            await servico.RegistrarAtividadeAsync(usuarioId);
            await escopoOperacao.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var persistido = await contextLeitura.Streaks.FindAsync(usuarioId);

        persistido.Should().NotBeNull();
        persistido!.DiasConsecutivos.Should().Be(5);
        persistido.UltimaAtividadeEm.Should().Be(hoje);
    }

    [TestMethod]
    public async Task RegistrarAtividadeAsync_HouveLacunaDeDias_ResetaSequenciaEPersisteNoBanco()
    {
        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        Guid usuarioId;
        using (var escopoSetup = CriarEscopo())
        {
            var contextSetup = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var usuario = await CriarUsuarioAsync(contextSetup);
            contextSetup.Streaks.Add(new Streak { UsuarioId = usuario.Id, DiasConsecutivos = 10, UltimaAtividadeEm = hoje.AddDays(-3) });
            await contextSetup.SaveChangesAsync();
            usuarioId = usuario.Id;
        }

        using (var escopoOperacao = CriarEscopo())
        {
            var servico = new StreakService(escopoOperacao.ServiceProvider.GetRequiredService<IStreakRepository>());
            await servico.RegistrarAtividadeAsync(usuarioId);
            await escopoOperacao.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var persistido = await contextLeitura.Streaks.FindAsync(usuarioId);

        persistido.Should().NotBeNull();
        persistido!.DiasConsecutivos.Should().Be(1);
        persistido.UltimaAtividadeEm.Should().Be(hoje);
    }
}
