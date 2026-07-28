using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Catalogo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Banca"/> para a tabela <c>bancas</c>.
/// </summary>
public class BancaConfiguration : IEntityTypeConfiguration<Banca>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Banca> builder)
    {
        builder.ToTable("bancas");
        builder.HasKey(b => b.Id);
    }
}
