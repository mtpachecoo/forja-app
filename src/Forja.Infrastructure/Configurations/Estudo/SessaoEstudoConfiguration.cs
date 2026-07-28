using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="SessaoEstudo"/> para a tabela <c>sessoes_estudo</c>.
/// </summary>
public class SessaoEstudoConfiguration : IEntityTypeConfiguration<SessaoEstudo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SessaoEstudo> builder)
    {
        builder.ToTable("sessoes_estudo");
        builder.HasKey(s => s.Id);
    }
}
