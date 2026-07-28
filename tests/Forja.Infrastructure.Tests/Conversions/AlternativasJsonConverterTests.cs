using Forja.Domain.Questoes;
using Forja.Infrastructure.Conversions;
using FluentAssertions;

namespace Forja.Infrastructure.Tests.Conversions;

[TestClass]
public class AlternativasJsonConverterTests
{
    private readonly AlternativasJsonConverter _converter = new();

    [TestMethod]
    public void ConvertToProvider_ComListaPreenchida_DeveSerializarComoObjetoLetraParaTexto()
    {
        // Formato real da coluna alternativas (jsonb) no banco: objeto {"A": "...", "B": "..."},
        // não um array de {Letra, Texto} — confirmado lendo uma questão real no Neon.
        var alternativas = new List<Alternativa>
        {
            new("A", "Primeira alternativa"),
            new("B", "Segunda alternativa"),
        };

        var resultado = _converter.ConvertToProvider(alternativas) as string;

        resultado.Should().NotBeNull();
        resultado.Should().Contain("\"A\":\"Primeira alternativa\"").And.Contain("\"B\":\"Segunda alternativa\"");
    }

    [TestMethod]
    public void ConvertToProvider_ComNull_DeveRetornarNull()
    {
        var resultado = _converter.ConvertToProvider(null);

        resultado.Should().BeNull();
    }

    [TestMethod]
    public void ConvertFromProvider_ComNull_DeveRetornarNull()
    {
        var resultado = _converter.ConvertFromProvider(null);

        resultado.Should().BeNull();
    }

    [TestMethod]
    public void ConvertFromProvider_ComListaVazia_DeveRetornarListaVazia()
    {
        var json = _converter.ConvertToProvider(new List<Alternativa>()) as string;

        var resultado = _converter.ConvertFromProvider(json) as List<Alternativa>;

        resultado.Should().NotBeNull();
        resultado.Should().BeEmpty();
    }

    [TestMethod]
    public void ConvertFromProvider_ComJsonNoFormatoRealDoBanco_DeveMapearParaAlternativas()
    {
        const string json = """{"A": "Texto A", "B": "Texto B", "C": "Texto C"}""";

        var resultado = _converter.ConvertFromProvider(json) as List<Alternativa>;

        resultado.Should().BeEquivalentTo(
        [
            new Alternativa("A", "Texto A"),
            new Alternativa("B", "Texto B"),
            new Alternativa("C", "Texto C"),
        ]);
    }

    [TestMethod]
    public void IdaEVolta_DevePreservarLetraETexto()
    {
        var original = new List<Alternativa>
        {
            new("A", "Texto da alternativa A"),
            new("B", "Texto da alternativa B"),
            new("C", "Texto da alternativa C"),
        };

        var json = _converter.ConvertToProvider(original) as string;
        var resultado = _converter.ConvertFromProvider(json) as List<Alternativa>;

        resultado.Should().BeEquivalentTo(original);
    }
}
