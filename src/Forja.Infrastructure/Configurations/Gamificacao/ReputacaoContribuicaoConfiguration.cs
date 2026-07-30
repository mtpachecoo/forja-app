using Forja.Domain.Gamificacao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Gamificacao;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="ReputacaoContribuicao"/> para a tabela
/// <c>reputacao_contribuicao</c>.
/// </summary>
public class ReputacaoContribuicaoConfiguration : IEntityTypeConfiguration<ReputacaoContribuicao>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReputacaoContribuicao> builder)
    {
        builder.ToTable("reputacao_contribuicao");
        builder.HasKey(r => r.UsuarioId);

        // Sem HasOne<Usuario>() explícito, mesmo precedente de StreakConfiguration/PontuacaoConfiguration:
        // UsuarioId já é a própria PK, e ReputacaoContribuicao nunca é criada na mesma chamada de
        // SaveChangesAsync que cria o Usuario — o bug de ordem de INSERT da lição da Fase 6 não se aplica aqui.
    }
}
