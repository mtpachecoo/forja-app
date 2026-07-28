using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Usuarios;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Usuario"/> para a tabela <c>usuarios</c>.
/// </summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);

        // CriadoEm nao e preenchido pelo codigo (fica default(DateTimeOffset) se nao setado
        // explicitamente); sem isso, o INSERT gravaria uma data invalida em vez de usar o
        // DEFAULT now() do Postgres.
        builder.Property(u => u.CriadoEm)
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }
}
