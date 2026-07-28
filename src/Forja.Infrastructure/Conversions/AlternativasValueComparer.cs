using Forja.Domain.Questoes;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Forja.Infrastructure.Conversions;

/// <summary>
/// Comparador de valores para <see cref="Questao.Alternativas"/>, garantindo que o change tracker do
/// EF Core compare o conteúdo da lista em vez da referência do objeto.
/// </summary>
public sealed class AlternativasValueComparer : ValueComparer<List<Alternativa>?>
{
    /// <summary>
    /// Cria uma nova instância do comparador.
    /// </summary>
    public AlternativasValueComparer() : base(
        (a, b) => AreEqual(a, b),
        v => v == null ? 0 : v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        v => v == null ? null : v.ToList())
    {
    }

    private static bool AreEqual(List<Alternativa>? a, List<Alternativa>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        return a.SequenceEqual(b);
    }
}
