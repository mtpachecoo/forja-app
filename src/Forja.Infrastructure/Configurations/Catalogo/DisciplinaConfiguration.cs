using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Catalogo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="Disciplina"/> para a tabela <c>disciplinas</c>.
/// </summary>
public class DisciplinaConfiguration : IEntityTypeConfiguration<Disciplina>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Disciplina> builder)
    {
        builder.ToTable("disciplinas");
        builder.HasKey(d => d.Id);
    }
}
