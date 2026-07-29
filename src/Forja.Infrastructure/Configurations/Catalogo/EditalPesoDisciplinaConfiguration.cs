using Forja.Domain.Catalogo;
using Forja.Infrastructure.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Catalogo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="EditalPesoDisciplina"/> para a tabela
/// <c>edital_peso_disciplina</c>, criada diretamente no banco (fora do fluxo de migrations do EF Core).
/// </summary>
public class EditalPesoDisciplinaConfiguration : IEntityTypeConfiguration<EditalPesoDisciplina>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EditalPesoDisciplina> builder)
    {
        builder.ToTable("edital_peso_disciplina");
        builder.HasKey(e => new { e.EditalId, e.DisciplinaId });

        builder.Property(e => e.Fonte)
            .HasConversion(new FontePesoDisciplinaConverter());
    }
}
