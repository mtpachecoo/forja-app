using Forja.Domain.Catalogo;
using Forja.Domain.Conteudo;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Forja.Infrastructure.ExternalAuth;
using Microsoft.EntityFrameworkCore;

namespace Forja.Infrastructure;

/// <summary>
/// Contexto do Entity Framework Core para o banco de dados do Forja, mapeando o schema
/// <c>public</c> já existente no Postgres/Neon.
/// </summary>
public class ForjaDbContext : DbContext
{
    /// <summary>
    /// Cria uma nova instância do contexto.
    /// </summary>
    /// <param name="options">Opções de configuração do contexto.</param>
    public ForjaDbContext(DbContextOptions<ForjaDbContext> options) : base(options)
    {
    }

    /// <summary>Usuários da plataforma.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Carreiras do catálogo.</summary>
    public DbSet<Carreira> Carreiras => Set<Carreira>();

    /// <summary>Bancas organizadoras.</summary>
    public DbSet<Banca> Bancas => Set<Banca>();

    /// <summary>Editais de concurso.</summary>
    public DbSet<Edital> Editais => Set<Edital>();

    /// <summary>Disciplinas de estudo.</summary>
    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();

    /// <summary>Tópicos de estudo.</summary>
    public DbSet<Topico> Topicos => Set<Topico>();

    /// <summary>Peso de cada disciplina dentro de um edital, para priorização do plano de estudo.</summary>
    public DbSet<EditalPesoDisciplina> EditalPesoDisciplina => Set<EditalPesoDisciplina>();

    /// <summary>Fontes de conteúdo.</summary>
    public DbSet<FonteConteudo> FontesConteudo => Set<FonteConteudo>();

    /// <summary>Chunks de conteúdo com embeddings.</summary>
    public DbSet<ChunkConteudo> ChunksConteudo => Set<ChunkConteudo>();

    /// <summary>Questões de estudo.</summary>
    public DbSet<Questao> Questoes => Set<Questao>();

    /// <summary>Planos de estudo.</summary>
    public DbSet<PlanoEstudo> PlanosEstudo => Set<PlanoEstudo>();

    /// <summary>Itens de plano de estudo.</summary>
    public DbSet<PlanoItem> PlanoItens => Set<PlanoItem>();

    /// <summary>Sessões de estudo.</summary>
    public DbSet<SessaoEstudo> SessoesEstudo => Set<SessaoEstudo>();

    /// <summary>Ciclos de pomodoro.</summary>
    public DbSet<Pomodoro> Pomodoros => Set<Pomodoro>();

    /// <summary>Respostas de usuários a questões.</summary>
    public DbSet<RespostaUsuario> RespostasUsuario => Set<RespostaUsuario>();

    /// <summary>Estados de revisão espaçada.</summary>
    public DbSet<RevisaoEspacada> RevisoesEspacadas => Set<RevisaoEspacada>();

    /// <summary>Sequências de dias consecutivos de estudo.</summary>
    public DbSet<Streak> Streaks => Set<Streak>();

    /// <summary>Pontuações dos usuários.</summary>
    public DbSet<Pontuacao> Pontuacoes => Set<Pontuacao>();

    /// <summary>Medalhas disponíveis.</summary>
    public DbSet<Medalha> Medalhas => Set<Medalha>();

    /// <summary>Medalhas conquistadas por usuários.</summary>
    public DbSet<UsuarioMedalha> UsuarioMedalhas => Set<UsuarioMedalha>();

    /// <summary>
    /// Usuários do Neon Auth (<c>neon_auth.user</c>), somente leitura — schema gerenciado externamente.
    /// </summary>
    public DbSet<NeonAuthUser> NeonAuthUsers => Set<NeonAuthUser>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<NivelUsuario>("nivel_usuario");
        modelBuilder.HasPostgresEnum<TipoQuestao>("tipo_questao");
        modelBuilder.HasPostgresEnum<StatusQuestao>("status_questao");
        modelBuilder.HasPostgresEnum<OrigemQuestao>("origem_questao");
        modelBuilder.HasPostgresEnum<TipoFonte>("tipo_fonte");
        modelBuilder.HasPostgresEnum<StatusItemPlano>("status_item_plano");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForjaDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
