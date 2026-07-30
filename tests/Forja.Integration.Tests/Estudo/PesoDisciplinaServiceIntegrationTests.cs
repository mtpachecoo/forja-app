using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Conteudo;
using Forja.Domain.Questoes;
using Forja.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Forja.Integration.Tests.Estudo;

/// <summary>
/// Testes de integração de <see cref="PesoDisciplinaService"/> contra um Postgres real
/// (Testcontainers), cobrindo os dois caminhos nunca exercitados contra dado real: (a) extração via
/// IA — que grava e relê o registro em <c>edital_peso_disciplina</c> — e (c) herança do edital
/// anterior, que exige as <em>duas</em> FKs de <c>edital_peso_disciplina</c> pra <c>editais</c>
/// (<c>edital_id</c> e <c>edital_origem_id</c>) funcionando corretamente ao mesmo tempo. O caso (b)
/// já era indiretamente exercitado por outros fluxos.
/// </summary>
[TestClass]
public class PesoDisciplinaServiceIntegrationTests : IntegrationTestBase
{
    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static PesoDisciplinaService CriarServico(IServiceScope escopo, IGeradorDeRespostaChat geradorDeRespostaChat) => new(
        escopo.ServiceProvider.GetRequiredService<IEditalPesoDisciplinaRepository>(),
        escopo.ServiceProvider.GetRequiredService<IEditalRepository>(),
        escopo.ServiceProvider.GetRequiredService<ITopicoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IDisciplinaRepository>(),
        escopo.ServiceProvider.GetRequiredService<IChunkConteudoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IQuestaoRepository>(),
        geradorDeRespostaChat,
        escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_CasoA_ExtraiViaIaEPersisteNoBanco()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var carreira = await CriarCarreiraAsync(context);
        var disciplina = await CriarDisciplinaAsync(context);
        var edital = new Edital { Id = Guid.NewGuid(), CarreiraId = carreira.Id, BancaId = await CriarBancaIdAsync(context), Ano = 2024, CriadoEm = DateTimeOffset.UtcNow };
        context.Editais.Add(edital);
        context.Topicos.Add(new Topico { Id = Guid.NewGuid(), EditalId = edital.Id, DisciplinaId = disciplina.Id, Nome = "Tópico Teste", Ordem = 1 });
        var fonte = new FonteConteudo { Id = Guid.NewGuid(), Tipo = TipoFonte.Edital, EditalId = edital.Id, Titulo = "Edital", TextoExtraido = "texto", VersaoEm = DateTimeOffset.UtcNow, CriadoEm = DateTimeOffset.UtcNow };
        context.FontesConteudo.Add(fonte);
        context.ChunksConteudo.Add(new ChunkConteudo
        {
            Id = Guid.NewGuid(),
            FonteId = fonte.Id,
            Texto = $"O edital terá 20 questões de {disciplina.Nome}.",
            Embedding = new float[1024],
            CriadoEm = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var jsonResposta = $$"""{"encontrado": true, "disciplinas": [{"nome": "{{disciplina.Nome}}", "peso": 20}]}""";
        var geradorDeRespostaChat = new Mock<IGeradorDeRespostaChat>();
        geradorDeRespostaChat
            .Setup(c => c.GerarRespostaAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResposta);

        var servico = CriarServico(escopo, geradorDeRespostaChat.Object);
        var resultado = await servico.ObterOuCalcularPesosAsync(edital.Id);

        resultado.Should().ContainSingle(p => p.DisciplinaId == disciplina.Id && p.Peso == 20m);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var persistido = await contextLeitura.EditalPesoDisciplina
            .AsNoTracking()
            .SingleAsync(p => p.EditalId == edital.Id && p.DisciplinaId == disciplina.Id);

        persistido.Peso.Should().Be(20m);
        persistido.Fonte.Should().Be(FontePesoDisciplina.ExtraidoEdital);
        persistido.EditalOrigemId.Should().BeNull();
    }

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_CasoC_HerdaDoEditalAnteriorEPersisteAmbasAsFks()
    {
        using var escopo = CriarEscopo();
        var context = escopo.ServiceProvider.GetRequiredService<ForjaDbContext>();

        var carreira = await CriarCarreiraAsync(context);
        var disciplina = await CriarDisciplinaAsync(context);
        var bancaId = await CriarBancaIdAsync(context);

        var editalAnterior = new Edital { Id = Guid.NewGuid(), CarreiraId = carreira.Id, BancaId = bancaId, Ano = 2022, CriadoEm = DateTimeOffset.UtcNow };
        var editalNovo = new Edital { Id = Guid.NewGuid(), CarreiraId = carreira.Id, BancaId = bancaId, Ano = 2024, CriadoEm = DateTimeOffset.UtcNow };
        context.Editais.AddRange(editalAnterior, editalNovo);
        context.Topicos.Add(new Topico { Id = Guid.NewGuid(), EditalId = editalNovo.Id, DisciplinaId = disciplina.Id, Nome = "Tópico Teste", Ordem = 1 });
        context.EditalPesoDisciplina.Add(new EditalPesoDisciplina
        {
            EditalId = editalAnterior.Id,
            DisciplinaId = disciplina.Id,
            Peso = 15m,
            Fonte = FontePesoDisciplina.ContagemQuestoes,
            CalculadoEm = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        // Sem chunk pro edital novo (caso a falha) e sem questão aprovada pra carreira (caso b falha).
        var servico = CriarServico(escopo, Mock.Of<IGeradorDeRespostaChat>());
        var resultado = await servico.ObterOuCalcularPesosAsync(editalNovo.Id);

        resultado.Should().ContainSingle(p => p.DisciplinaId == disciplina.Id && p.Peso == 15m);

        using var escopoLeitura = CriarEscopo();
        var contextLeitura = escopoLeitura.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var persistido = await contextLeitura.EditalPesoDisciplina
            .AsNoTracking()
            .SingleAsync(p => p.EditalId == editalNovo.Id && p.DisciplinaId == disciplina.Id);

        persistido.Peso.Should().Be(15m);
        persistido.Fonte.Should().Be(FontePesoDisciplina.HerdadoEditalAnterior);
        persistido.EditalOrigemId.Should().Be(editalAnterior.Id);
    }

    private static async Task<Guid> CriarBancaIdAsync(ForjaDbContext context)
    {
        var banca = new Banca { Id = Guid.NewGuid(), Nome = $"Banca Teste {Guid.NewGuid():N}", CriadoEm = DateTimeOffset.UtcNow };
        context.Bancas.Add(banca);
        await context.SaveChangesAsync();
        return banca.Id;
    }
}
