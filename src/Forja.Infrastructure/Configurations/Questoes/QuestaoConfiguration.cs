using Forja.Domain.Questoes;
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
    }
}
