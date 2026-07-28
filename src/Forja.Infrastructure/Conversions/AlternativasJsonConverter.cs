using System.Text.Json;
using Forja.Domain.Questoes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Forja.Infrastructure.Conversions;

/// <summary>
/// Converte <see cref="Questao.Alternativas"/> (<see cref="List{Alternativa}"/>) de/para a coluna
/// <c>alternativas</c> (<c>jsonb</c>). No banco, o formato é um objeto JSON de letra para texto
/// (ex.: <c>{"A": "...", "B": "..."}</c>), não um array — este conversor faz a ponte entre os dois formatos.
/// </summary>
public sealed class AlternativasJsonConverter : ValueConverter<List<Alternativa>?, string?>
{
    /// <summary>
    /// Cria uma nova instância do conversor.
    /// </summary>
    public AlternativasJsonConverter() : base(
        v => v == null ? null : Serialize(v),
        v => v == null ? null : Deserialize(v))
    {
    }

    private static string Serialize(List<Alternativa> alternativas)
    {
        var porLetra = alternativas.ToDictionary(a => a.Letra, a => a.Texto);
        return JsonSerializer.Serialize(porLetra, (JsonSerializerOptions?)null);
    }

    private static List<Alternativa> Deserialize(string json)
    {
        var porLetra = JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
            ?? [];
        return porLetra.Select(par => new Alternativa(par.Key, par.Value)).ToList();
    }
}
