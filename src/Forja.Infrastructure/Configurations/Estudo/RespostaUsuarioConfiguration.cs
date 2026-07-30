using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="RespostaUsuario"/> para a tabela <c>respostas_usuario</c>.
/// </summary>
public class RespostaUsuarioConfiguration : IEntityTypeConfiguration<RespostaUsuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RespostaUsuario> builder)
    {
        builder.ToTable("respostas_usuario");
        builder.HasKey(r => r.Id);

        // Índice composto pra ExisteRespostaPontuadaAsync (usuario_id + questao_id) — já existe no
        // Neon desde antes da adoção de EF Core Migrations (criado fora do fluxo de migrations, como
        // edital_peso_disciplina), nunca tinha sido mapeado no modelo. Mapear pelo nome real evita que
        // o dotnet ef tente recriá-lo com outro nome ou dropar índices de FK que nunca existiram de
        // fato (o baseline assumia por convenção, mas nunca foram criados — Up() do InicioBaseline
        // ficou vazio de propósito). Não único: múltiplas respostas por (usuario, questão) são
        // esperadas (tentativas erradas, revisões) — só a primeira correta pontua, é regra de negócio,
        // não constraint de dado.
        builder.HasIndex(r => new { r.UsuarioId, r.QuestaoId }).HasDatabaseName("idx_respostas_usuario_questao");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(r => r.UsuarioId)
            .IsRequired();

        builder.HasOne<Questao>()
            .WithMany()
            .HasForeignKey(r => r.QuestaoId)
            .IsRequired();

        builder.HasOne<Pomodoro>()
            .WithMany()
            .HasForeignKey(r => r.PomodoroId)
            .IsRequired(false);
    }
}
