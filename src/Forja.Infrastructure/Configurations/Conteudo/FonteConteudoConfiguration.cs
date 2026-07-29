using Forja.Domain.Catalogo;
using Forja.Domain.Conteudo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Conteudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="FonteConteudo"/> para a tabela <c>fontes_conteudo</c>.
/// </summary>
public class FonteConteudoConfiguration : IEntityTypeConfiguration<FonteConteudo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FonteConteudo> builder)
    {
        builder.ToTable("fontes_conteudo");
        builder.HasKey(f => f.Id);

        builder.HasOne<Edital>()
            .WithMany()
            .HasForeignKey(f => f.EditalId)
            .IsRequired(false);
    }
}
