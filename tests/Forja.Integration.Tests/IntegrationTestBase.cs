using Forja.Domain.Catalogo;
using Forja.Domain.Usuarios;
using Forja.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Forja.Integration.Tests;

/// <summary>
/// Base para testes de integração que precisam de um Postgres real com o schema do Forja aplicado.
/// Um único container Postgres/pgvector é compartilhado por toda a suíte (subida cara, ~poucos
/// segundos) — os testes rodam sequencialmente (ver <c>MSTestSettings.cs</c>) e cada um limpa o banco
/// em <see cref="LimparBancoAsync"/> antes de rodar, garantindo isolamento sem precisar de um
/// container por teste.
/// </summary>
[TestClass]
public abstract class IntegrationTestBase
{
    private const string Usuario = "forja_test";
    private const string Senha = "forja_test";
    private const string Banco = "forja_test";

    private static PostgreSqlContainer _container = null!;
    private static ServiceProvider _serviceProvider = null!;

    /// <inheritdoc cref="Microsoft.VisualStudio.TestTools.UnitTesting.TestContext" />
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Sobe o container Postgres/pgvector e aplica <c>schema-atual.sql</c> uma única vez para toda a suíte.</summary>
    [AssemblyInitialize]
    public static async Task AssemblyInitializeAsync(TestContext context)
    {
        _container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
            .WithDatabase(Banco)
            .WithUsername(Usuario)
            .WithPassword(Senha)
            .Build();

        await _container.StartAsync();
        await AplicarSchemaAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ForjaDb"] =
                    $"postgresql://{Usuario}:{Senha}@{_container.Hostname}:{_container.GetMappedPublicPort(5432)}/{Banco}",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>Encerra o container ao final da suíte.</summary>
    [AssemblyCleanup]
    public static async Task AssemblyCleanupAsync()
    {
        _serviceProvider.Dispose();
        await _container.DisposeAsync();
    }

    private static async Task AplicarSchemaAsync()
    {
        var script = await File.ReadAllTextAsync(LocalizarSchemaSql());

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = script;
        await command.ExecuteNonQueryAsync();
    }

    private static string LocalizarSchemaSql()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "schema-atual.sql")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new FileNotFoundException("schema-atual.sql não encontrado na raiz do repositório.")
            : Path.Combine(directory.FullName, "schema-atual.sql");
    }

    /// <summary>Cria um escopo de DI com repositórios reais (<see cref="ForjaDbContext"/> incluso) contra o container Postgres.</summary>
    protected static IServiceScope CriarEscopo() => _serviceProvider.CreateScope();

    /// <summary>
    /// Remove todos os dados das tabelas da aplicação, isolando cada teste. <c>TRUNCATE ... CASCADE</c>
    /// a partir das tabelas "raiz" arrasta todas as dependentes mesmo quando a FK real é
    /// <c>ON DELETE RESTRICT</c>/<c>SET NULL</c> — CASCADE do TRUNCATE ignora a ação configurada na FK.
    /// </summary>
    protected static async Task LimparBancoAsync()
    {
        using var scope = CriarEscopo();
        var context = scope.ServiceProvider.GetRequiredService<ForjaDbContext>();
        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE neon_auth."user", public.carreiras, public.bancas, public.disciplinas, public.editais, public.medalhas
            RESTART IDENTITY CASCADE
            """);
    }

    /// <summary>
    /// Insere um usuário de teste, incluindo o registro correspondente em <c>neon_auth.user</c> — a FK
    /// <c>fk_usuarios_neon_auth</c> exige que ele exista antes do <c>usuarios</c>.
    /// </summary>
    protected static async Task<Usuario> CriarUsuarioAsync(ForjaDbContext context)
    {
        var id = Guid.NewGuid();
        var email = $"forja-{id:N}@teste.forja";

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""INSERT INTO neon_auth."user" (id, name, email, "emailVerified") VALUES ({id}, {"Usuário Teste"}, {email}, true)""");

        var usuario = new Usuario
        {
            Id = id,
            Email = email,
            Nome = "Usuário Teste",
            Nivel = NivelUsuario.Intermediario,
            TempoDisponivelMinDia = 60,
            CriadoEm = DateTimeOffset.UtcNow,
        };
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        return usuario;
    }

    /// <summary>Insere uma carreira de teste.</summary>
    protected static async Task<Carreira> CriarCarreiraAsync(ForjaDbContext context)
    {
        var carreira = new Carreira
        {
            Id = Guid.NewGuid(),
            Nome = $"Carreira Teste {Guid.NewGuid():N}",
            Orgao = "Órgão Teste",
            CriadoEm = DateTimeOffset.UtcNow,
        };
        context.Carreiras.Add(carreira);
        await context.SaveChangesAsync();
        return carreira;
    }

    /// <summary>Insere uma disciplina de teste.</summary>
    protected static async Task<Disciplina> CriarDisciplinaAsync(ForjaDbContext context)
    {
        var disciplina = new Disciplina { Id = Guid.NewGuid(), Nome = $"Disciplina Teste {Guid.NewGuid():N}" };
        context.Disciplinas.Add(disciplina);
        await context.SaveChangesAsync();
        return disciplina;
    }
}
