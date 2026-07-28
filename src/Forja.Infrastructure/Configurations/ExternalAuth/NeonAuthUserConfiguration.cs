using Forja.Infrastructure.ExternalAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.ExternalAuth;

/// <summary>
/// Mapeamento Fluent API, somente leitura, da tabela <c>neon_auth.user</c>. Essa tabela é gerenciada
/// externamente pelo Neon Auth e usa colunas em camelCase (ex.: <c>"emailVerified"</c>), diferente do
/// restante do banco (snake_case) — por isso os nomes de coluna são sobrescritos explicitamente aqui,
/// já que a convenção snake_case global do <see cref="ForjaDbContext"/> não se aplica a esta tabela.
/// </summary>
public class NeonAuthUserConfiguration : IEntityTypeConfiguration<NeonAuthUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NeonAuthUser> builder)
    {
        builder.ToTable("user", "neon_auth");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Name).HasColumnName("name");
        builder.Property(u => u.Email).HasColumnName("email");
        builder.Property(u => u.EmailVerified).HasColumnName("emailVerified");
    }
}
