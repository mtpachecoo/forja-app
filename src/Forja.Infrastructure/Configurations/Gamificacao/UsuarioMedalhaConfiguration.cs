using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Gamificacao;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="UsuarioMedalha"/> para a tabela <c>usuario_medalhas</c>.
/// </summary>
public class UsuarioMedalhaConfiguration : IEntityTypeConfiguration<UsuarioMedalha>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UsuarioMedalha> builder)
    {
        builder.ToTable("usuario_medalhas");
        builder.HasKey(u => new { u.UsuarioId, u.MedalhaId });
    }
}
