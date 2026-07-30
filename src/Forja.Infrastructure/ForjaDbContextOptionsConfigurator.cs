using Forja.Domain.Conteudo;
using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure;

/// <summary>
/// Configuração do <see cref="ForjaDbContext"/> compartilhada entre o registro em tempo de execução
/// (<see cref="DependencyInjection.AddInfrastructure"/>) e a criação em tempo de design usada pelas
/// ferramentas <c>dotnet ef</c> (<see cref="ForjaDbContextFactory"/>).
/// </summary>
internal static class ForjaDbContextOptionsConfigurator
{
    /// <summary>
    /// Aplica o provider Npgsql, os mapeamentos de enum do Postgres e a convenção snake_case.
    /// </summary>
    /// <param name="options">Builder de opções do contexto a configurar.</param>
    /// <param name="connectionString">Connection string no formato nativo do Npgsql.</param>
    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MapEnum<NivelUsuario>("nivel_usuario")
                .MapEnum<TipoQuestao>("tipo_questao")
                .MapEnum<StatusQuestao>("status_questao")
                .MapEnum<OrigemQuestao>("origem_questao")
                .MapEnum<TipoFonte>("tipo_fonte")
                .MapEnum<StatusItemPlano>("status_item_plano")
                .UseVector())
            .UseSnakeCaseNamingConvention();
    }
}
