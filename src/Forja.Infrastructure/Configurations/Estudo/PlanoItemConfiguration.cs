using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="PlanoItem"/> para a tabela <c>plano_itens</c>.
/// </summary>
public class PlanoItemConfiguration : IEntityTypeConfiguration<PlanoItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanoItem> builder)
    {
        builder.ToTable("plano_itens");
        builder.HasKey(p => p.Id);
    }
}
