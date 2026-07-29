using Forja.Domain.Estudo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Forja.Infrastructure.Configurations.Estudo;

/// <summary>
/// Mapeamento Fluent API da entidade <see cref="PlanoItem"/> para a tabela <c>plano_itens</c>.
/// </summary>
public class PlanoItemConfiguration : IEntityTypeConfiguration<PlanoItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlanoItem> builder)
    {
        builder.ToTable("plano_itens");
        builder.HasKey(p => p.Id);

        // PlanoItem não tem navegação pra PlanoEstudo (POCOs sem navegação, por convenção deste
        // projeto) — sem isso, o EF Core não sabe que precisa inserir o PlanoEstudo antes dos seus
        // PlanoItem quando os dois são adicionados na mesma chamada de SaveChangesAsync (como faz
        // PlanoEstudoService), e a ordem de INSERT pode violar a FK do banco.
        builder.HasOne<PlanoEstudo>()
            .WithMany()
            .HasForeignKey(p => p.PlanoId)
            .IsRequired();
    }
}
