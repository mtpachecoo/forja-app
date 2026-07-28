using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Catalogo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Carreira"/> para a tabela <c>carreiras</c>.
/// </summary>
public class CarreiraConfiguration : IEntityTypeConfiguration<Carreira>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Carreira> builder)
    {
        builder.ToTable("carreiras");
        builder.HasKey(c => c.Id);
    }
}
