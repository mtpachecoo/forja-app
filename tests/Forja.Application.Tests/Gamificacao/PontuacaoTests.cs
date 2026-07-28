using Forja.Domain.Gamificacao;
using FluentAssertions;

namespace Forja.Application.Tests.Gamificacao;

[TestClass]
public class PontuacaoTests
{
    [TestMethod]
    public void RegistrarPontos_PrimeiraVez_AcumulaTotalESemanaAPartirDeZero()
    {
        var pontuacao = new Pontuacao { UsuarioId = Guid.NewGuid() };

        pontuacao.RegistrarPontos(10, new DateOnly(2026, 7, 27)); // segunda-feira

        pontuacao.PontosTotal.Should().Be(10);
        pontuacao.PontosSemanaAtual.Should().Be(10);
        pontuacao.SemanaReferencia.Should().Be(new DateOnly(2026, 7, 27));
    }

    [TestMethod]
    public void RegistrarPontos_MesmaSemana_AcumulaNaSemanaAtual()
    {
        var pontuacao = new Pontuacao { UsuarioId = Guid.NewGuid() };

        pontuacao.RegistrarPontos(10, new DateOnly(2026, 7, 27)); // segunda
        pontuacao.RegistrarPontos(10, new DateOnly(2026, 7, 30)); // quinta, mesma semana

        pontuacao.PontosTotal.Should().Be(20);
        pontuacao.PontosSemanaAtual.Should().Be(20);
        pontuacao.SemanaReferencia.Should().Be(new DateOnly(2026, 7, 27));
    }

    [TestMethod]
    public void RegistrarPontos_AvancaParaSemanaSeguinte_ResetaSemanaAtualMasAcumulaTotal()
    {
        var pontuacao = new Pontuacao { UsuarioId = Guid.NewGuid() };

        pontuacao.RegistrarPontos(10, new DateOnly(2026, 7, 27)); // segunda da semana 1
        pontuacao.RegistrarPontos(10, new DateOnly(2026, 7, 30)); // quinta da semana 1
        pontuacao.RegistrarPontos(10, new DateOnly(2026, 8, 3));  // segunda da semana seguinte

        pontuacao.PontosTotal.Should().Be(30);
        pontuacao.PontosSemanaAtual.Should().Be(10);
        pontuacao.SemanaReferencia.Should().Be(new DateOnly(2026, 8, 3));
    }
}
