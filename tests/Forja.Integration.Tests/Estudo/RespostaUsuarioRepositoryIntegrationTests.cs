using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Estudo;

/// <summary>
/// Teste de integração de <see cref="Forja.Infrastructure.Repositories.RespostaUsuarioRepository.GetHistoricoPorDisciplinaAsync"/>
/// contra um Postgres real (Testcontainers) — usa <c>Database.SqlQuery&lt;T&gt;</c> com SQL cru
/// (<c>ROW_NUMBER() OVER (PARTITION BY ...)</c>) para limitar o histórico por disciplina direto no
/// banco, no lugar de carregar tudo e descartar em memória. Cobertura mockada (<c>AnaliseDesempenhoServiceTests</c>)
/// não valida se essa sintaxe SQL ou o mapeamento de colunas para <see cref="RespostaDisciplina"/> realmente funcionam.
/// </summary>
[TestClass]
public class RespostaUsuarioRepositoryIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    [TestMethod]
    public async Task GetHistoricoPorDisciplinaAsync_LimitaPorDisciplinaViaSql()
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

        // 5 respostas para a mesma disciplina, em momentos distintos.
        var momento = DateTimeOffset.UtcNow.AddDays(-5);
        for (var i = 0; i < 5; i++)
        {
            context.RespostasUsuario.Add(new RespostaUsuario
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                QuestaoId = questao.Id,
                RespostaDada = "certo",
                Correta = i % 2 == 0,
                TempoRespostaMs = 1000,
                CriadoEm = momento.AddDays(i),
            });
        }
        await context.SaveChangesAsync();

        var repositorio = escopo.ServiceProvider.GetRequiredService<IRespostaUsuarioRepository>();
        var historico = await repositorio.GetHistoricoPorDisciplinaAsync(usuario.Id, limitePorDisciplina: 3);

        historico.Should().HaveCount(3, "o limite por disciplina deve ser aplicado no próprio SQL, não depois em memória");
        historico.Should().OnlyContain(r => r.DisciplinaId == disciplina.Id);
        // As 3 mais recentes das 5 são i=2,3,4 (momento + 2,3,4 dias) -> Correta = i%2==0 => true,false,true.
        historico.OrderBy(r => r.CriadoEm).Select(r => r.Correta).Should().Equal(true, false, true);
    }
}
