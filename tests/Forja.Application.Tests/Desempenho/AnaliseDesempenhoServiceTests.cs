using Forja.Application.Desempenho;
using Forja.Domain.Catalogo;
using Forja.Domain.Estudo;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Desempenho;

[TestClass]
public class AnaliseDesempenhoServiceTests
{
    private readonly Mock<IRespostaUsuarioRepository> _respostaRepository = new();
    private readonly Mock<IDisciplinaRepository> _disciplinaRepository = new();
    private readonly AnaliseDesempenhoService _service;

    public AnaliseDesempenhoServiceTests()
    {
        _service = new AnaliseDesempenhoService(_respostaRepository.Object, _disciplinaRepository.Object);
    }

    private static List<RespostaDisciplina> CriarJanelas(Guid disciplinaId, int acertosAnteriores, int acertosRecentes, int tamanhoJanela = 10)
    {
        var respostas = new List<RespostaDisciplina>();
        var momento = DateTimeOffset.UtcNow.AddDays(-20);

        for (var i = 0; i < tamanhoJanela; i++)
        {
            respostas.Add(new RespostaDisciplina(disciplinaId, Correta: i < acertosAnteriores, momento));
            momento = momento.AddHours(1);
        }

        for (var i = 0; i < tamanhoJanela; i++)
        {
            respostas.Add(new RespostaDisciplina(disciplinaId, Correta: i < acertosRecentes, momento));
            momento = momento.AddHours(1);
        }

        return respostas;
    }

    [TestMethod]
    public async Task DetectarQuedaDeDesempenhoAsync_QuedaReal_DisparaAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        // Anterior: 8/10 = 80%. Recente: 3/10 = 30%. Queda de 50 pontos percentuais.
        _respostaRepository.Setup(r => r.GetHistoricoPorDisciplinaAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarJanelas(disciplinaId, acertosAnteriores: 8, acertosRecentes: 3));
        _disciplinaRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Disciplina { Id = disciplinaId, Nome = "Direito Constitucional" }]);

        var alertas = await _service.DetectarQuedaDeDesempenhoAsync(usuarioId);

        alertas.Should().ContainSingle();
        alertas[0].DisciplinaId.Should().Be(disciplinaId);
        alertas[0].TaxaAcertoAnterior.Should().Be(80m);
        alertas[0].TaxaAcertoRecente.Should().Be(30m);
        alertas[0].Mensagem.Should().Contain("Direito Constitucional");
    }

    [TestMethod]
    public async Task DetectarQuedaDeDesempenhoAsync_TaxaEstavel_NaoDisparaAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        // Anterior: 8/10 = 80%. Recente: 7/10 = 70%. Queda de so 10 pontos, abaixo do limiar de 15.
        _respostaRepository.Setup(r => r.GetHistoricoPorDisciplinaAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarJanelas(disciplinaId, acertosAnteriores: 8, acertosRecentes: 7));

        var alertas = await _service.DetectarQuedaDeDesempenhoAsync(usuarioId);

        alertas.Should().BeEmpty();
    }

    [TestMethod]
    public async Task DetectarQuedaDeDesempenhoAsync_TaxaEmAlta_NaoDisparaAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        // Anterior: 3/10 = 30%. Recente: 9/10 = 90%. Melhorou bastante.
        _respostaRepository.Setup(r => r.GetHistoricoPorDisciplinaAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarJanelas(disciplinaId, acertosAnteriores: 3, acertosRecentes: 9));

        var alertas = await _service.DetectarQuedaDeDesempenhoAsync(usuarioId);

        alertas.Should().BeEmpty();
    }

    [TestMethod]
    public async Task DetectarQuedaDeDesempenhoAsync_AmostraInsuficiente_NaoDisparaAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        // So 6 respostas no total (3 pontos de queda simulados) - nao da pra formar as duas janelas de 10.
        var respostas = new List<RespostaDisciplina>
        {
            new(disciplinaId, true, DateTimeOffset.UtcNow.AddHours(-6)),
            new(disciplinaId, true, DateTimeOffset.UtcNow.AddHours(-5)),
            new(disciplinaId, true, DateTimeOffset.UtcNow.AddHours(-4)),
            new(disciplinaId, false, DateTimeOffset.UtcNow.AddHours(-3)),
            new(disciplinaId, false, DateTimeOffset.UtcNow.AddHours(-2)),
            new(disciplinaId, false, DateTimeOffset.UtcNow.AddHours(-1)),
        };
        _respostaRepository.Setup(r => r.GetHistoricoPorDisciplinaAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(respostas);

        var alertas = await _service.DetectarQuedaDeDesempenhoAsync(usuarioId);

        alertas.Should().BeEmpty();
    }
}
