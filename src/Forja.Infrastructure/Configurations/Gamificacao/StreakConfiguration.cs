using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Gamificacao;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Streak"/> para a tabela <c>streaks</c>.
/// </summary>
public class StreakConfiguration : IEntityTypeConfiguration<Streak>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Streak> builder)
    {
        builder.ToTable("streaks");
        builder.HasKey(s => s.UsuarioId);
    }
}
