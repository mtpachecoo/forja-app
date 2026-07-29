using Forja.Application.Estudo;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Estudo;

/// <summary>
/// Testes de integração de <see cref="RevisaoEspacadaService"/> contra um Postgres real
/// (Testcontainers), confirmando que <c>erros_consecutivos</c> e <c>proxima_revisao_em</c> são
/// gravados corretamente — não apenas que o serviço não lança exceção.
/// </summary>
[TestClass]
public class RevisaoEspacadaServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    [TestMethod]
    public async Task RegistrarRespostaAsync_ErroDepoisAcerto_GravaErrosConsecutivosEProximaRevisaoCorretos()
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
        await context.SaveChangesAsync();

        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var unitOfWork = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var servico = new RevisaoEspacadaService(escopo.ServiceProvider.GetRequiredService<IRevisaoEspacadaRepository>());

        // Primeiro erro: insere, erros=1, intervalo mínimo (1 dia).
        await servico.RegistrarRespostaAsync(usuario.Id, questao.Id, correta: false);
        await unitOfWork.SaveChangesAsync();

        using (var escopoLeitura1 = CriarEscopo())
        {
            var revisao = await escopoLeitura1.ServiceProvider.GetRequiredService<ForjaDbContext>().RevisoesEspacadas
                .SingleAsync(r => r.UsuarioId == usuario.Id && r.QuestaoId == questao.Id);
            revisao.ErrosConsecutivos.Should().Be(1);
            revisao.ProximaRevisaoEm.Should().Be(hoje.AddDays(1));
        }

        // Segundo erro consecutivo: atualiza (não duplica, respeitando a UNIQUE(usuario_id, questao_id)), erros=2.
        await servico.RegistrarRespostaAsync(usuario.Id, questao.Id, correta: false);
        await unitOfWork.SaveChangesAsync();

        using (var escopoLeitura2 = CriarEscopo())
        {
            var revisao = await escopoLeitura2.ServiceProvider.GetRequiredService<ForjaDbContext>().RevisoesEspacadas
                .SingleAsync(r => r.UsuarioId == usuario.Id && r.QuestaoId == questao.Id);
            revisao.ErrosConsecutivos.Should().Be(2);
            revisao.ProximaRevisaoEm.Should().Be(hoje.AddDays(1));
        }

        // Acerto: zera erros consecutivos e dobra o intervalo (1 -> 2 dias).
        await servico.RegistrarRespostaAsync(usuario.Id, questao.Id, correta: true);
        await unitOfWork.SaveChangesAsync();

        using var escopoLeitura3 = CriarEscopo();
        var revisaoFinal = await escopoLeitura3.ServiceProvider.GetRequiredService<ForjaDbContext>().RevisoesEspacadas
            .SingleAsync(r => r.UsuarioId == usuario.Id && r.QuestaoId == questao.Id);
        revisaoFinal.ErrosConsecutivos.Should().Be(0);
        revisaoFinal.ProximaRevisaoEm.Should().Be(hoje.AddDays(2));
    }
}
