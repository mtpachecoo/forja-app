using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="RevisaoEspacada"/> para a tabela <c>revisao_espacada</c>.
/// </summary>
public class RevisaoEspacadaConfiguration : IEntityTypeConfiguration<RevisaoEspacada>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RevisaoEspacada> builder)
    {
        builder.ToTable("revisao_espacada");
        builder.HasKey(r => r.Id);

        // Índice composto pra GetByUsuarioIdEQuestaoIdAsync (usuario_id + questao_id) — já existe no
        // Neon como UNIQUE CONSTRAINT desde antes da adoção de EF Core Migrations, nunca tinha sido
        // mapeado no modelo (mesmo caso de respostas_usuario/idx_respostas_usuario_questao). A unique
        // constraint já reforça a nível de banco a regra de negócio que RevisaoEspacadaService só
        // aplica em memória (checar-então-criar): no máximo uma revisão espaçada por (usuario, questão).
        builder.HasIndex(r => new { r.UsuarioId, r.QuestaoId })
            .IsUnique()
            .HasDatabaseName("revisao_espacada_usuario_id_questao_id_key");

        // Índice pra ObterPendentesAsync (usuario_id + proxima_revisao_em) — também já existia fora do
        // fluxo de migrations, mapeado aqui pela mesma razão.
        builder.HasIndex(r => new { r.UsuarioId, r.ProximaRevisaoEm }).HasDatabaseName("idx_revisao_pendente");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(r => r.UsuarioId)
            .IsRequired();

        builder.HasOne<Questao>()
            .WithMany()
            .HasForeignKey(r => r.QuestaoId)
            .IsRequired();
    }
}
