using Forja.Domain.Catalogo;
using Forja.Domain.Conteudo;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Forja.Infrastructure.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Questoes;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Questao"/> para a tabela <c>questoes</c>.
/// </summary>
public class QuestaoConfiguration : IEntityTypeConfiguration<Questao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Questao> builder)
    {
        builder.ToTable("questoes");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Alternativas)
            .HasConversion(new AlternativasJsonConverter())
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(new AlternativasValueComparer());

        builder.HasOne<Carreira>()
            .WithMany()
            .HasForeignKey(q => q.CarreiraId)
            .IsRequired();

        builder.HasOne<Banca>()
            .WithMany()
            .HasForeignKey(q => q.BancaId)
            .IsRequired(false);

        builder.HasOne<Disciplina>()
            .WithMany()
            .HasForeignKey(q => q.DisciplinaId)
            .IsRequired();

        builder.HasOne<Topico>()
            .WithMany()
            .HasForeignKey(q => q.TopicoId)
            .IsRequired(false);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(q => q.RevisadoPor)
            .IsRequired(false);

        builder.HasOne<FonteConteudo>()
            .WithMany()
            .HasForeignKey(q => q.FonteProvaId)
            .IsRequired(false);
    }
}
