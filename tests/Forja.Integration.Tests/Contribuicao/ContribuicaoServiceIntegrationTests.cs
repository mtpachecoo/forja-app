using Forja.Application.Common;
using Forja.Application.Contribuicao;
using Forja.Domain.Common;
using Forja.Domain.Contribuicao;
using Forja.Domain.Gamificacao;
using Forja.Infrastructure;
using Forja.Infrastructure.ReadStores;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Forja.Integration.Tests.Contribuicao;

/// <summary>
/// Teste de integração de <see cref="ContribuicaoService"/> e <see cref="PerfilPublicoReadStore"/>
/// contra um Postgres real (Testcontainers) — cobre os 3 fluxos: submeter, moderar (aprovar/rejeitar)
/// e visualizar perfil público.
/// </summary>
[TestClass]
public class ContribuicaoServiceIntegrationTests : IntegrationTestBase
{
    private const string EmailAdmin = "admin@forja.teste";

    [TestInitialize]
    public async Task TestInitialize() => await LimparBancoAsync();

    private static ContribuicaoService CriarServico(IServiceScope escopo, IAdminAuthorizer adminAuthorizer) => new(
        escopo.ServiceProvider.GetRequiredService<IContribuicaoConteudoRepository>(),
        escopo.ServiceProvider.GetRequiredService<Forja.Domain.Catalogo.ITopicoRepository>(),
        escopo.ServiceProvider.GetRequiredService<Forja.Domain.Usuarios.IUsuarioRepository>(),
        escopo.ServiceProvider.GetRequiredService<IReputacaoContribuicaoRepository>(),
        escopo.ServiceProvider.GetRequiredService<IUsuarioMedalhaRepository>(),
        adminAuthorizer,
        escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

    private static Mock<IAdminAuthorizer> CriarAdminAuthorizer()
    {
        var mock = new Mock<IAdminAuthorizer>();
        mock.Setup(a => a.EhAdmin(EmailAdmin)).Returns(true);
        mock.Setup(a => a.EhAdmin(It.Is<string>(e => e != EmailAdmin))).Returns(false);
        return mock;
    }

    [TestMethod]
    public async Task FluxoCompleto_SubmeterAprovarERejeitar_ModeraCorretamenteEAtualizaReputacaoEPerfil()
    {
        Guid autorId, topicoId, adminId;
        using (var escopoSetup = CriarEscopo())
        {
            var context = escopoSetup.ServiceProvider.GetRequiredService<ForjaDbContext>();
            var autor = await CriarUsuarioAsync(context);
            var admin = await CriarUsuarioAsync(context);
            admin.Email = EmailAdmin;
            await context.SaveChangesAsync();

            var disciplina = await CriarDisciplinaAsync(context);
            var carreira = await CriarCarreiraAsync(context);
            var banca = new Forja.Domain.Catalogo.Banca { Id = Guid.NewGuid(), Nome = $"Banca {Guid.NewGuid():N}", CriadoEm = DateTimeOffset.UtcNow };
            context.Bancas.Add(banca);
            var edital = new Forja.Domain.Catalogo.Edital { Id = Guid.NewGuid(), CarreiraId = carreira.Id, BancaId = banca.Id, Ano = 2024, CriadoEm = DateTimeOffset.UtcNow };
            context.Editais.Add(edital);
            var topico = new Forja.Domain.Catalogo.Topico { Id = Guid.NewGuid(), EditalId = edital.Id, DisciplinaId = disciplina.Id, Nome = "Tópico", Ordem = 1 };
            context.Topicos.Add(topico);
            await context.SaveChangesAsync();

            autorId = autor.Id;
            adminId = admin.Id;
            topicoId = topico.Id;
        }

        var adminAuthorizer = CriarAdminAuthorizer();

        // Submete duas contribuições do mesmo autor.
        Guid contribuicaoAprovadaId, contribuicaoRejeitadaId;
        using (var escopoSubmissao = CriarEscopo())
        {
            var servico = CriarServico(escopoSubmissao, adminAuthorizer.Object);
            var c1 = await servico.SubmeterAsync(autorId, topicoId, TipoContribuicao.Video, "https://exemplo.com/video1");
            var c2 = await servico.SubmeterAsync(autorId, topicoId, TipoContribuicao.Pdf, "https://exemplo.com/doc.pdf");
            contribuicaoAprovadaId = c1.Id;
            contribuicaoRejeitadaId = c2.Id;
        }

        // Em revisão: não aparece na listagem de aprovadas.
        using (var escopoLeitura1 = CriarEscopo())
        {
            var servico = CriarServico(escopoLeitura1, adminAuthorizer.Object);
            var aprovadas = await servico.ListarAprovadasPorTopicoAsync(topicoId, 0, 50);
            aprovadas.Should().BeEmpty();
        }

        // Aprova uma, rejeita a outra.
        using (var escopoModeracao = CriarEscopo())
        {
            var servico = CriarServico(escopoModeracao, adminAuthorizer.Object);
            await servico.AprovarAsync(contribuicaoAprovadaId, adminId);
            await servico.RejeitarAsync(contribuicaoRejeitadaId, adminId);
        }

        // Só a aprovada aparece na listagem pública.
        using (var escopoLeitura2 = CriarEscopo())
        {
            var servico = CriarServico(escopoLeitura2, adminAuthorizer.Object);
            var aprovadas = await servico.ListarAprovadasPorTopicoAsync(topicoId, 0, 50);
            aprovadas.Should().ContainSingle(c => c.Id == contribuicaoAprovadaId);
        }

        // Perfil público reflete a reputação e a medalha de marco concedidas pela aprovação.
        using var escopoPerfil = CriarEscopo();
        var readStore = escopoPerfil.ServiceProvider.GetRequiredService<Forja.Domain.Usuarios.IPerfilPublicoReadStore>();
        var perfil = await readStore.ObterPorUsuarioIdAsync(autorId);

        perfil.Should().NotBeNull();
        perfil!.ContribuicoesAprovadas.Should().Be(1);
        perfil.NivelReputacao.Should().Be(NivelReputacao.Bronze, "10 pontos de uma única aprovação fica abaixo do limiar de Prata");
        perfil.Badges.Should().ContainSingle();

        var contextFinal = escopoPerfil.ServiceProvider.GetRequiredService<ForjaDbContext>();
        var reputacao = await contextFinal.ReputacoesContribuicao.AsNoTracking().FirstAsync(r => r.UsuarioId == autorId);
        reputacao.PontosContribuicao.Should().Be(10);
    }
}
