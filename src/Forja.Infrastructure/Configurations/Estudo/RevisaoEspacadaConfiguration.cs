using Forja.Domain.Estudo;
using Forja.Domain.Questoes;
using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="RevisaoEspacada"/> para a tabela <c>revisao_espacada</c>.
/// </summary>
public class RevisaoEspacadaConfiguration : IEntityTypeConfiguration<RevisaoEspacada>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RevisaoEspacada> builder)
    {
        builder.ToTable("revisao_espacada");
        builder.HasKey(r => r.Id);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(r => r.UsuarioId)
            .IsRequired();

        builder.HasOne<Questao>()
            .WithMany()
            .HasForeignKey(r => r.QuestaoId)
            .IsRequired();
    }
}
