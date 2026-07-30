using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Contribuicao;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Forja.Domain.Conteudo;
using Forja.Infrastructure.ReadStores;
using Forja.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forja.Infrastructure;

/// <summary>
/// Métodos de extensão para registrar os serviços de <see cref="Forja.Infrastructure"/> no container de DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra o <see cref="ForjaDbContext"/> e os repositórios do Domain no container de DI.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação, contendo a connection string <c>ForjaDb</c>.</param>
    /// <returns>A própria coleção de serviços, para encadeamento.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = NeonConnectionStringConverter.ToNpgsqlConnectionString(
            configuration.GetConnectionString("ForjaDb")!);

        services.AddDbContext<ForjaDbContext>(options => ForjaDbContextOptionsConfigurator.Configure(options, connectionString));

        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IQuestaoRepository, QuestaoRepository>();
        services.AddScoped<IRespostaUsuarioRepository, RespostaUsuarioRepository>();
        services.AddScoped<IChunkConteudoRepository, ChunkConteudoRepository>();
        services.AddScoped<IPlanoEstudoRepository, PlanoEstudoRepository>();
        services.AddScoped<IPlanoItemRepository, PlanoItemRepository>();
        services.AddScoped<ISessaoEstudoRepository, SessaoEstudoRepository>();
        services.AddScoped<IPomodoroRepository, PomodoroRepository>();
        services.AddScoped<IRevisaoEspacadaRepository, RevisaoEspacadaRepository>();
        services.AddScoped<IPontuacaoRepository, PontuacaoRepository>();
        services.AddScoped<IStreakRepository, StreakRepository>();
        services.AddScoped<IIdentidadeExternaRepository, NeonAuthUserRepository>();
        services.AddScoped<IEditalRepository, EditalRepository>();
        services.AddScoped<IDisciplinaRepository, DisciplinaRepository>();
        services.AddScoped<ITopicoRepository, TopicoRepository>();
        services.AddScoped<IEditalPesoDisciplinaRepository, EditalPesoDisciplinaRepository>();
        services.AddScoped<IContribuicaoConteudoRepository, ContribuicaoConteudoRepository>();
        services.AddScoped<IReputacaoContribuicaoRepository, ReputacaoContribuicaoRepository>();
        services.AddScoped<IMedalhaRepository, MedalhaRepository>();
        services.AddScoped<IUsuarioMedalhaRepository, UsuarioMedalhaRepository>();
        services.AddScoped<IPerfilPublicoReadStore, PerfilPublicoReadStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
