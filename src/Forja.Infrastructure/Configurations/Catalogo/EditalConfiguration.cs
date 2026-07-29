using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Catalogo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Edital"/> para a tabela <c>editais</c>.
/// </summary>
public class EditalConfiguration : IEntityTypeConfiguration<Edital>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Edital> builder)
    {
        builder.ToTable("editais");
        builder.HasKey(e => e.Id);

        builder.HasOne<Carreira>()
            .WithMany()
            .HasForeignKey(e => e.CarreiraId)
            .IsRequired();

        builder.HasOne<Banca>()
            .WithMany()
            .HasForeignKey(e => e.BancaId)
            .IsRequired();
    }
}
