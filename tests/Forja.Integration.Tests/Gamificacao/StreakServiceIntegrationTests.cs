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
/// <see cref="Streak.UltimaAtividadeEm"/> já persistida, em vez de mockar o relógio.
/// </summary>
[TestClass]
public class StreakServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    [TestMethod]
    public async Task RegistrarAtividadeAsync_UltimaAtividadeFoiOntem_IncrementaEPersisteNoBanco()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var usuario = await CriarUsuarioAsync(context);

        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        context.Streaks.Add(new Streak { UsuarioId = usuario.Id, DiasConsecutivos = 4, UltimaAtividadeEm = hoje.AddDays(-1) });
        await context.SaveChangesAsync();

        var servico = new StreakService(escopo.ServiceProvider.GetRequiredService<IStreakRepository>());
        await servico.RegistrarAtividadeAsync(usuario.Id);
        await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var persistido = await contextLeitura.Streaks.FindAsync(usuario.Id);

        persistido.Should().NotBeNull();
        persistido!.DiasConsecutivos.Should().Be(5);
        persistido.UltimaAtividadeEm.Should().Be(hoje);
    }

    [TestMethod]
    public async Task RegistrarAtividadeAsync_HouveLacunaDeDias_ResetaSequenciaEPersisteNoBanco()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var usuario = await CriarUsuarioAsync(context);

        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        context.Streaks.Add(new Streak { UsuarioId = usuario.Id, DiasConsecutivos = 10, UltimaAtividadeEm = hoje.AddDays(-3) });
        await context.SaveChangesAsync();

        var servico = new StreakService(escopo.ServiceProvider.GetRequiredService<IStreakRepository>());
        await servico.RegistrarAtividadeAsync(usuario.Id);
        await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var persistido = await contextLeitura.Streaks.FindAsync(usuario.Id);

        persistido.Should().NotBeNull();
        persistido!.DiasConsecutivos.Should().Be(1);
        persistido.UltimaAtividadeEm.Should().Be(hoje);
    }
}
