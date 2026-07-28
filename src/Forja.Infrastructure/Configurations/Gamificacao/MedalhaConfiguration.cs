using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Gamificacao;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Medalha"/> para a tabela <c>medalhas</c>.
/// </summary>
public class MedalhaConfiguration : IEntityTypeConfiguration<Medalha>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Medalha> builder)
    {
        builder.ToTable("medalhas");
        builder.HasKey(m => m.Id);
    }
}
