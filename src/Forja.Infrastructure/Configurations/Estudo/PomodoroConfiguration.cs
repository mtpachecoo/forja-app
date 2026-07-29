using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Pomodoro"/> para a tabela <c>pomodoros</c>.
/// </summary>
public class PomodoroConfiguration : IEntityTypeConfiguration<Pomodoro>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Pomodoro> builder)
    {
        builder.ToTable("pomodoros");
        builder.HasKey(p => p.Id);

        builder.HasOne<SessaoEstudo>()
            .WithMany()
            .HasForeignKey(p => p.SessaoId)
            .IsRequired();
    }
}
