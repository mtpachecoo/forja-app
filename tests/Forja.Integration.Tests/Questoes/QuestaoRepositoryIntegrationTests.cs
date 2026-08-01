using Forja.Domain.Catalogo;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Integration.Tests.Questoes;

/// <summary>
/// Teste de integração de <see cref="Forja.Infrastructure.Repositories.QuestaoRepository.GetByFiltroAsync"/>
/// contra um Postgres real (Testcontainers). Cobertura mockada (<c>QuestaoRepositoryTests</c>, se houver, ou
/// os serviços que o consomem) verifica apenas que a chamada foi feita com os parâmetros certos — não que a
/// árvore de <c>Where</c>s condicionais monta o SQL certo e o Postgres devolve o conjunto certo de linhas
/// para cada combinação de filtro.
/// </summary>
[TestClass]
public class QuestaoRepositoryIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static async Task<Banca> CriarBancaAsync(ForjaDbContext context)
    {
        var banca = new Banca { Id = Guid.NewGuid(), Nome = $"Banca Teste {Guid.NewGuid():N}", CriadoEm = DateTimeOffset.UtcNow };
        context.Bancas.Add(banca);
        await context.SaveChangesAsync();
        return banca;
    }

    private static Questao NovaQuestao(Guid carreiraId, Guid bancaId, Guid disciplinaId) => new()
    {
        Id = Guid.NewGuid(),
        CarreiraId = carreiraId,
        BancaId = bancaId,
        DisciplinaId = disciplinaId,
        Tipo = TipoQuestao.CertoErrado,
        Enunciado = "Enunciado de teste",
        Gabarito = "certo",
        Explicacao = "Explicação de teste",
        Status = StatusQuestao.Aprovada,
        CriadoEm = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// Semeia 5 questões cruzando 2 carreiras, 2 bancas e 2 disciplinas, de forma que cada filtro
    /// isolado e cada combinação testada abaixo recorte um subconjunto diferente e inequívoco.
    /// </summary>
    private static async Task<(Guid CarreiraA, Guid CarreiraB, Guid BancaA, Guid BancaB, Guid DisciplinaA, Guid DisciplinaB,
        Questao Q1CarreiraABancaADisciplinaA,
        Questao Q2CarreiraABancaBDisciplinaA,
        Questao Q3CarreiraABancaADisciplinaB,
        Questao Q4CarreiraBBancaADisciplinaA,
        Questao Q5CarreiraBBancaBDisciplinaB)> SemearQuestoesAsync(ForjaDbContext context)
    {
        var carreiraA = await CriarCarreiraAsync(context);
        var carreiraB = await CriarCarreiraAsync(context);
        var bancaA = await CriarBancaAsync(context);
        var bancaB = await CriarBancaAsync(context);
        var disciplinaA = await CriarDisciplinaAsync(context);
        var disciplinaB = await CriarDisciplinaAsync(context);

        var q1 = NovaQuestao(carreiraA.Id, bancaA.Id, disciplinaA.Id);
        var q2 = NovaQuestao(carreiraA.Id, bancaB.Id, disciplinaA.Id);
        var q3 = NovaQuestao(carreiraA.Id, bancaA.Id, disciplinaB.Id);
        var q4 = NovaQuestao(carreiraB.Id, bancaA.Id, disciplinaA.Id);
        var q5 = NovaQuestao(carreiraB.Id, bancaB.Id, disciplinaB.Id);

        context.Questoes.AddRange(q1, q2, q3, q4, q5);
        await context.SaveChangesAsync();

        return (carreiraA.Id, carreiraB.Id, bancaA.Id, bancaB.Id, disciplinaA.Id, disciplinaB.Id, q1, q2, q3, q4, q5);
    }

    [TestMethod]
    public async Task GetByFiltroAsync_ApenasCarreira_RetornaTodasDaCarreiraIndependenteDeBancaEDisciplina()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var (carreiraA, _, _, _, _, _, q1, q2, q3, _, _) = await SemearQuestoesAsync(context);

        var repositorio = escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>();
        var resultado = await repositorio.GetByFiltroAsync(carreiraId: carreiraA, bancaId: null, disciplinaId: null, status: null, skip: 0, take: 50);

        resultado.Select(q => q.Id).Should().BeEquivalentTo([q1.Id, q2.Id, q3.Id]);
    }

    [TestMethod]
    public async Task GetByFiltroAsync_ApenasBanca_RetornaTodasDaBancaIndependenteDeCarreiraEDisciplina()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var (_, _, bancaA, _, _, _, q1, _, q3, q4, _) = await SemearQuestoesAsync(context);

        var repositorio = escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>();
        var resultado = await repositorio.GetByFiltroAsync(carreiraId: null, bancaId: bancaA, disciplinaId: null, status: null, skip: 0, take: 50);

        resultado.Select(q => q.Id).Should().BeEquivalentTo([q1.Id, q3.Id, q4.Id]);
    }

    [TestMethod]
    public async Task GetByFiltroAsync_ApenasDisciplina_RetornaTodasDaDisciplinaIndependenteDeCarreiraEBanca()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var (_, _, _, _, disciplinaA, _, q1, q2, _, q4, _) = await SemearQuestoesAsync(context);

        var repositorio = escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>();
        var resultado = await repositorio.GetByFiltroAsync(carreiraId: null, bancaId: null, disciplinaId: disciplinaA, status: null, skip: 0, take: 50);

        resultado.Select(q => q.Id).Should().BeEquivalentTo([q1.Id, q2.Id, q4.Id]);
    }

    [TestMethod]
    public async Task GetByFiltroAsync_CarreiraEBanca_RecortaInterseccaoDosDoisFiltros()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var (carreiraA, _, bancaA, _, _, _, q1, _, q3, _, _) = await SemearQuestoesAsync(context);

        var repositorio = escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>();
        var resultado = await repositorio.GetByFiltroAsync(carreiraId: carreiraA, bancaId: bancaA, disciplinaId: null, status: null, skip: 0, take: 50);

        resultado.Select(q => q.Id).Should().BeEquivalentTo([q1.Id, q3.Id]);
    }

    [TestMethod]
    public async Task GetByFiltroAsync_CarreiraBancaEDisciplina_RecortaInterseccaoDosTresFiltros()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var (carreiraA, _, bancaA, _, disciplinaA, _, q1, _, _, _, _) = await SemearQuestoesAsync(context);

        var repositorio = escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>();
        var resultado = await repositorio.GetByFiltroAsync(carreiraId: carreiraA, bancaId: bancaA, disciplinaId: disciplinaA, status: null, skip: 0, take: 50);

        resultado.Select(q => q.Id).Should().BeEquivalentTo([q1.Id]);
    }
}
