using Forja.Domain.Catalogo;
using Forja.Domain.Contribuicao;
using Forja.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Contribuicao;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="ContribuicaoConteudo"/> para a tabela
/// <c>contribuicoes_conteudo</c>.
/// </summary>
public class ContribuicaoConteudoConfiguration : IEntityTypeConfiguration<ContribuicaoConteudo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContribuicaoConteudo> builder)
    {
        builder.ToTable("contribuicoes_conteudo");
        builder.HasKey(c => c.Id);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .IsRequired();

        builder.HasOne<Topico>()
            .WithMany()
            .HasForeignKey(c => c.TopicoId)
            .IsRequired();

        // Segunda referência a Usuario na mesma tabela (autor em UsuarioId, moderador em ModeradoPor)
        // — mesmo padrão de duas FKs pra mesma tabela já usado em EditalPesoDisciplinaConfiguration
        // (edital_id/edital_origem_id).
        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.ModeradoPor)
            .IsRequired(false);
    }
}
