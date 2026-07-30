using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Forja.Infrastructure;

/// <summary>
/// Cria o <see cref="ForjaDbContext"/> em tempo de design para as ferramentas <c>dotnet ef</c>, sem
/// depender do host da API (que exige configuração adicional — Gemini, Voyage, NeonAuth — irrelevante
/// para gerar ou aplicar migrations). Lê a connection string apenas de variáveis de ambiente, na mesma
/// convenção usada em runtime (<c>ConnectionStrings__ForjaDb</c>).
/// </summary>
public class ForjaDbContextFactory : IDesignTimeDbContextFactory<ForjaDbContext>
{
    /// <inheritdoc />
    public ForjaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = NeonConnectionStringConverter.ToNpgsqlConnectionString(
            configuration.GetConnectionString("ForjaDb")
            ?? throw new InvalidOperationException(
                "Defina a variável de ambiente ConnectionStrings__ForjaDb para usar o dotnet ef."));

        var optionsBuilder = new DbContextOptionsBuilder<ForjaDbContext>();
        ForjaDbContextOptionsConfigurator.Configure(optionsBuilder, connectionString);

        return new ForjaDbContext(optionsBuilder.Options);
    }
}
