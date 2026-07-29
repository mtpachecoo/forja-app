using Forja.Application.Gamificacao;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Usuarios;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Gamificacao;

[TestClass]
public class PontuacaoServiceTests
{
    private readonly Mock<IPontuacaoRepository> _pontuacaoRepository = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IPlanoEstudoRepository> _planoEstudoRepository = new();
    private readonly PontuacaoService _service;

    public PontuacaoServiceTests()
    {
        _service = new PontuacaoService(_pontuacaoRepository.Object, _usuarioRepository.Object, _planoEstudoRepository.Object);
    }

    [TestMethod]
    public async Task ObterRankingSemanalAsync_SemFiltroDeCarreira_RetornaTodosOrdenados()
    {
        var usuario1 = Guid.NewGuid();
        var usuario2 = Guid.NewGuid();
        var inicioSemana = Pontuacao.InicioDaSemana(DateOnly.FromDateTime(DateTime.UtcNow));

        _pontuacaoRepository.Setup(r => r.GetRankingSemanalAsync(inicioSemana, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Pontuacao { UsuarioId = usuario1, PontosSemanaAtual = 50, SemanaReferencia = inicioSemana },
                new Pontuacao { UsuarioId = usuario2, PontosSemanaAtual = 30, SemanaReferencia = inicioSemana },
            ]);
        _usuarioRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Usuario { Id = usuario1, Nome = "Alice" },
                new Usuario { Id = usuario2, Nome = "Bob" },
            ]);

        var resultado = await _service.ObterRankingSemanalAsync(carreiraId: null);

        resultado.Should().HaveCount(2);
        resultado[0].UsuarioId.Should().Be(usuario1);
        resultado[0].Posicao.Should().Be(1);
        resultado[1].Posicao.Should().Be(2);
        _planoEstudoRepository.Verify(r => r.GetByCarreiraIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ObterRankingSemanalAsync_ComFiltroDeCarreira_RestringeAosUsuariosDaCarreira()
    {
        var carreiraId = Guid.NewGuid();
        var usuarioDaCarreira = Guid.NewGuid();
        var usuarioDeOutraCarreira = Guid.NewGuid();
        var inicioSemana = Pontuacao.InicioDaSemana(DateOnly.FromDateTime(DateTime.UtcNow));

        _pontuacaoRepository.Setup(r => r.GetRankingSemanalAsync(inicioSemana, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Pontuacao { UsuarioId = usuarioDaCarreira, PontosSemanaAtual = 20, SemanaReferencia = inicioSemana },
                new Pontuacao { UsuarioId = usuarioDeOutraCarreira, PontosSemanaAtual = 100, SemanaReferencia = inicioSemana },
            ]);
        _planoEstudoRepository.Setup(r => r.GetByCarreiraIdAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioDaCarreira, CarreiraId = carreiraId }]);
        _usuarioRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Usuario { Id = usuarioDaCarreira, Nome = "Alice" }]);

        var resultado = await _service.ObterRankingSemanalAsync(carreiraId);

        resultado.Should().ContainSingle(r => r.UsuarioId == usuarioDaCarreira);
    }
}
