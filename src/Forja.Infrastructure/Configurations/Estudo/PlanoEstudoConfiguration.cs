using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="PlanoEstudo"/> para a tabela <c>planos_estudo</c>.
/// </summary>
public class PlanoEstudoConfiguration : IEntityTypeConfiguration<PlanoEstudo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanoEstudo> builder)
    {
        builder.ToTable("planos_estudo");
        builder.HasKey(p => p.Id);
    }
}
