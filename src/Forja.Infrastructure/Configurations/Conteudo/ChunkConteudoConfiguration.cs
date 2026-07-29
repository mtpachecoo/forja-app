using Forja.Domain.Catalogo;
using Forja.Domain.Conteudo;
using Forja.Infrastructure.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Conteudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="ChunkConteudo"/> para a tabela <c>chunks_conteudo</c>.
/// </summary>
public class ChunkConteudoConfiguration : IEntityTypeConfiguration<ChunkConteudo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChunkConteudo> builder)
    {
        builder.ToTable("chunks_conteudo");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Embedding)
            .HasConversion(new EmbeddingVectorConverter());

        builder.HasOne<FonteConteudo>()
            .WithMany()
            .HasForeignKey(c => c.FonteId)
            .IsRequired();

        builder.HasOne<Topico>()
            .WithMany()
            .HasForeignKey(c => c.TopicoId)
            .IsRequired(false);
    }
}
