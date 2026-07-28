using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Gamificacao;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Pontuacao"/> para a tabela <c>pontuacoes</c>.
/// </summary>
public class PontuacaoConfiguration : IEntityTypeConfiguration<Pontuacao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Pontuacao> builder)
    {
        builder.ToTable("pontuacoes");
        builder.HasKey(p => p.UsuarioId);
    }
}
