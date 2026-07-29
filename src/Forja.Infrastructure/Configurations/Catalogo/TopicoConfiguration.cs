using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Catalogo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Topico"/> para a tabela <c>topicos</c>.
/// </summary>
public class TopicoConfiguration : IEntityTypeConfiguration<Topico>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Topico> builder)
    {
        builder.ToTable("topicos");
        builder.HasKey(t => t.Id);

        builder.HasOne<Edital>()
            .WithMany()
            .HasForeignKey(t => t.EditalId)
            .IsRequired();

        builder.HasOne<Disciplina>()
            .WithMany()
            .HasForeignKey(t => t.DisciplinaId)
            .IsRequired();
    }
}
