using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="RespostaUsuario"/> para a tabela <c>respostas_usuario</c>.
/// </summary>
public class RespostaUsuarioConfiguration : IEntityTypeConfiguration<RespostaUsuario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RespostaUsuario> builder)
    {
        builder.ToTable("respostas_usuario");
        builder.HasKey(r => r.Id);
    }
}
