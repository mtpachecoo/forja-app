using Forja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Forja.Infrastructure.Conversions;

/// <summary>
/// Converte <see cref="FontePesoDisciplina"/> de/para os literais exatos da coluna <c>fonte</c>
/// (<c>text</c>, não um enum nativo do Postgres) em <c>edital_peso_disciplina</c>.
/// </summary>
public sealed class FontePesoDisciplinaConverter : ValueConverter<FontePesoDisciplina, string>
{
    /// <summary>
    /// Cria uma nova instância do conversor.
    /// </summary>
    public FontePesoDisciplinaConverter() : base(
        v => ParaTexto(v),
        v => ParaEnum(v))
    {
    }

    private static string ParaTexto(FontePesoDisciplina fonte) => fonte switch
    {
        FontePesoDisciplina.ExtraidoEdital => "extraido_edital",
        FontePesoDisciplina.ContagemQuestoes => "contagem_questoes",
        FontePesoDisciplina.HerdadoEditalAnterior => "herdado_edital_anterior",
        _ => throw new ArgumentOutOfRangeException(nameof(fonte), fonte, "Fonte de peso de disciplina desconhecida."),
    };

    private static FontePesoDisciplina ParaEnum(string texto) => texto switch
    {
        "extraido_edital" => FontePesoDisciplina.ExtraidoEdital,
        "contagem_questoes" => FontePesoDisciplina.ContagemQuestoes,
        "herdado_edital_anterior" => FontePesoDisciplina.HerdadoEditalAnterior,
        _ => throw new ArgumentOutOfRangeException(nameof(texto), texto, "Valor de 'fonte' desconhecido em edital_peso_disciplina."),
    };
}
