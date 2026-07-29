using Forja.Application.Estudo;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Conteudo;
using Forja.Domain.Questoes;
using FluentAssertions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class PesoDisciplinaServiceTests
{
    private readonly Mock<IEditalPesoDisciplinaRepository> _pesoRepository = new();
    private readonly Mock<IEditalRepository> _editalRepository = new();
    private readonly Mock<ITopicoRepository> _topicoRepository = new();
    private readonly Mock<IDisciplinaRepository> _disciplinaRepository = new();
    private readonly Mock<IChunkConteudoRepository> _chunkRepository = new();
    private readonly Mock<IQuestaoRepository> _questaoRepository = new();
    private readonly Mock<IChatCompletionService> _chatCompletionService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PesoDisciplinaService _service;

    public PesoDisciplinaServiceTests()
    {
        _service = new PesoDisciplinaService(
            _pesoRepository.Object,
            _editalRepository.Object,
            _topicoRepository.Object,
            _disciplinaRepository.Object,
            _chunkRepository.Object,
            _questaoRepository.Object,
            _chatCompletionService.Object,
            _unitOfWork.Object);
    }

    private void ConfigurarSemPesosExistentes(Guid editalId)
        => _pesoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

    private void ConfigurarEditalETopicos(Guid editalId, Guid carreiraId, params (Guid TopicoId, Guid DisciplinaId)[] topicos)
    {
        _editalRepository.Setup(r => r.GetByIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Edital { Id = editalId, CarreiraId = carreiraId });

        _topicoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topicos.Select(t => new Topico { Id = t.TopicoId, EditalId = editalId, DisciplinaId = t.DisciplinaId }).ToList());
    }

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_JaExisteCalculado_RetornaSemRecalcular()
    {
        var editalId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        _pesoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EditalPesoDisciplina { EditalId = editalId, DisciplinaId = disciplinaId, Peso = 42m, Fonte = FontePesoDisciplina.ContagemQuestoes }]);

        var resultado = await _service.ObterOuCalcularPesosAsync(editalId);

        resultado.Should().ContainSingle(p => p.DisciplinaId == disciplinaId && p.Peso == 42m);
        _editalRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_CasoA_ExtraiViaIaQuandoEditalDeclaraDistribuicao()
    {
        var editalId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var direitoConstitucionalId = Guid.NewGuid();
        var direitoAdministrativoId = Guid.NewGuid();

        ConfigurarSemPesosExistentes(editalId);
        ConfigurarEditalETopicos(editalId, carreiraId,
            (Guid.NewGuid(), direitoConstitucionalId),
            (Guid.NewGuid(), direitoAdministrativoId));

        _chunkRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ChunkConteudo { Id = Guid.NewGuid(), FonteId = Guid.NewGuid(), Texto = "O edital tera 20 questoes de Direito Constitucional e 10 de Direito Administrativo." }]);

        _disciplinaRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Disciplina { Id = direitoConstitucionalId, Nome = "Direito Constitucional" },
                new Disciplina { Id = direitoAdministrativoId, Nome = "Direito Administrativo" },
            ]);

        const string jsonResposta = """
            {"encontrado": true, "disciplinas": [{"nome": "Direito Constitucional", "peso": 20}, {"nome": "Direito Administrativo", "peso": 10}]}
            """;
        _chatCompletionService
            .Setup(c => c.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ChatMessageContent(AuthorRole.Assistant, jsonResposta)]);

        var resultado = await _service.ObterOuCalcularPesosAsync(editalId);

        resultado.Should().Contain(p => p.DisciplinaId == direitoConstitucionalId && p.Peso == 20m);
        resultado.Should().Contain(p => p.DisciplinaId == direitoAdministrativoId && p.Peso == 10m);

        _pesoRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<EditalPesoDisciplina>>(l => l.All(p => p.Fonte == FontePesoDisciplina.ExtraidoEdital) && l.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _questaoRepository.Verify(r => r.ContarAprovadasPorDisciplinaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_CasoB_AmostraSuficiente_UsaContagemComoPeso()
    {
        var editalId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();

        ConfigurarSemPesosExistentes(editalId);
        ConfigurarEditalETopicos(editalId, carreiraId, (Guid.NewGuid(), disciplinaId));
        _chunkRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _questaoRepository.Setup(r => r.ContarAprovadasPorDisciplinaAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [disciplinaId] = 37 });

        var resultado = await _service.ObterOuCalcularPesosAsync(editalId);

        resultado.Should().ContainSingle(p => p.DisciplinaId == disciplinaId && p.Peso == 37m);
        _pesoRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<EditalPesoDisciplina>>(l => l.Single().Fonte == FontePesoDisciplina.ContagemQuestoes),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_CasoB_AmostraInsuficiente_UsaPesoIgualEntreDisciplinasDeBaixaAmostra()
    {
        var editalId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var disciplinaA = Guid.NewGuid();
        var disciplinaB = Guid.NewGuid();

        ConfigurarSemPesosExistentes(editalId);
        ConfigurarEditalETopicos(editalId, carreiraId, (Guid.NewGuid(), disciplinaA), (Guid.NewGuid(), disciplinaB));
        _chunkRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        // Ambas abaixo do piso de 10 (uma delas com 0 questoes — nem aparece no dicionario).
        _questaoRepository.Setup(r => r.ContarAprovadasPorDisciplinaAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [disciplinaA] = 3 });

        var resultado = await _service.ObterOuCalcularPesosAsync(editalId);

        var pesoA = resultado.Single(p => p.DisciplinaId == disciplinaA).Peso;
        var pesoB = resultado.Single(p => p.DisciplinaId == disciplinaB).Peso;
        pesoA.Should().Be(pesoB, "disciplinas de baixa amostra devem ter peso igual entre si");
        pesoA.Should().Be(10m);
    }

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_CasoC_HerdaPesosDoEditalAnteriorQuandoNaoHaChunkNemQuestao()
    {
        var editalId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var editalAnteriorId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();

        ConfigurarSemPesosExistentes(editalId);
        ConfigurarEditalETopicos(editalId, carreiraId, (Guid.NewGuid(), disciplinaId));
        _chunkRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _questaoRepository.Setup(r => r.ContarAprovadasPorDisciplinaAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        _pesoRepository.Setup(r => r.ObterPesosDoEditalAnteriorMaisRecenteAsync(carreiraId, editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EditalPesoDisciplina { EditalId = editalAnteriorId, DisciplinaId = disciplinaId, Peso = 15m, Fonte = FontePesoDisciplina.ContagemQuestoes }]);

        var resultado = await _service.ObterOuCalcularPesosAsync(editalId);

        resultado.Should().ContainSingle(p => p.DisciplinaId == disciplinaId && p.Peso == 15m);
        _pesoRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<EditalPesoDisciplina>>(l =>
                l.Single().Fonte == FontePesoDisciplina.HerdadoEditalAnterior &&
                l.Single().EditalOrigemId == editalAnteriorId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ObterOuCalcularPesosAsync_CasoD_SemHistoricoNenhum_RetornaPesoIgualSemPersistir()
    {
        var editalId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var disciplinaA = Guid.NewGuid();
        var disciplinaB = Guid.NewGuid();

        ConfigurarSemPesosExistentes(editalId);
        ConfigurarEditalETopicos(editalId, carreiraId, (Guid.NewGuid(), disciplinaA), (Guid.NewGuid(), disciplinaB));
        _chunkRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _questaoRepository.Setup(r => r.ContarAprovadasPorDisciplinaAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        _pesoRepository.Setup(r => r.ObterPesosDoEditalAnteriorMaisRecenteAsync(carreiraId, editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await _service.ObterOuCalcularPesosAsync(editalId);

        resultado.Should().HaveCount(2);
        resultado.Select(p => p.Peso).Distinct().Should().ContainSingle();

        _pesoRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<EditalPesoDisciplina>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
