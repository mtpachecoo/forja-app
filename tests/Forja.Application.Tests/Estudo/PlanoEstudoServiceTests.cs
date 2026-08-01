using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Usuarios;
using FluentAssertions;
using Moq;

namespace Forja.Application.Tests.Estudo;

[TestClass]
public class PlanoEstudoServiceTests
{
    private readonly Mock<IPlanoEstudoRepository> _planoEstudoRepository = new();
    private readonly Mock<IPlanoItemRepository> _planoItemRepository = new();
    private readonly Mock<IEditalRepository> _editalRepository = new();
    private readonly Mock<ITopicoRepository> _topicoRepository = new();
    private readonly Mock<IDisciplinaRepository> _disciplinaRepository = new();
    private readonly Mock<IPesoDisciplinaService> _pesoDisciplinaService = new();
    private readonly Mock<IGeradorDeRespostaChat> _geradorDeRespostaChat = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PlanoEstudoService _service;

    public PlanoEstudoServiceTests()
    {
        _service = new PlanoEstudoService(
            _planoEstudoRepository.Object,
            _planoItemRepository.Object,
            _editalRepository.Object,
            _topicoRepository.Object,
            _disciplinaRepository.Object,
            _pesoDisciplinaService.Object,
            _geradorDeRespostaChat.Object,
            _unitOfWork.Object);
    }

    private void ConfigurarSemPlanoExistente(Guid usuarioId)
        => _planoEstudoRepository.Setup(r => r.GetByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

    private void ConfigurarRespostaLlm(string json)
    {
        _geradorDeRespostaChat
            .Setup(c => c.GerarRespostaAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);
    }

    [TestMethod]
    public async Task ObterOuGerarPlanoAtualAsync_PlanoJaExiste_RetornaSemGerarDeNovo()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var plano = new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, CarreiraId = carreiraId, CriadoEm = DateTimeOffset.UtcNow };

        _planoEstudoRepository.Setup(r => r.GetByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync([plano]);
        _planoItemRepository.Setup(r => r.GetByPlanoIdAsync(plano.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var resultado = await _service.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, 60, NivelUsuario.Iniciante);

        resultado.Plano.Should().BeSameAs(plano);
        _editalRepository.Verify(r => r.GetMaisRecentePorCarreiraAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ObterOuGerarPlanoAtualAsync_NuncaIncluiTopicoForaDoCatalogo()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var editalId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        var topicoRealId = Guid.NewGuid();
        var topicoAlucinadoId = Guid.NewGuid(); // nao existe no catalogo do edital

        ConfigurarSemPlanoExistente(usuarioId);
        _editalRepository.Setup(r => r.GetMaisRecentePorCarreiraAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Edital { Id = editalId, CarreiraId = carreiraId });
        _topicoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Topico { Id = topicoRealId, EditalId = editalId, DisciplinaId = disciplinaId, Nome = "Topico real", Ordem = 1 }]);
        _disciplinaRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Disciplina { Id = disciplinaId, Nome = "Disciplina real" }]);
        _pesoDisciplinaService.Setup(s => s.ObterOuCalcularPesosAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PesoDisciplina(disciplinaId, 10m)]);

        var jsonResposta = $$"""
            {"itens": [
                {"topicoId": "{{topicoRealId}}", "tempoAlocadoMin": 30},
                {"topicoId": "{{topicoAlucinadoId}}", "tempoAlocadoMin": 30}
            ]}
            """;
        ConfigurarRespostaLlm(jsonResposta);

        var resultado = await _service.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, 60, NivelUsuario.Iniciante);

        resultado.Itens.Should().ContainSingle();
        resultado.Itens[0].TopicoId.Should().Be(topicoRealId);
    }

    [TestMethod]
    public async Task ObterOuGerarPlanoAtualAsync_TempoAlocadoAcimaDoDisponivel_EhClampadoParaOTempoDisponivel()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var editalId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        var topicoId = Guid.NewGuid();
        const int tempoDisponivel = 60;

        ConfigurarSemPlanoExistente(usuarioId);
        _editalRepository.Setup(r => r.GetMaisRecentePorCarreiraAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Edital { Id = editalId, CarreiraId = carreiraId });
        _topicoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Topico { Id = topicoId, EditalId = editalId, DisciplinaId = disciplinaId, Nome = "Topico", Ordem = 1 }]);
        _disciplinaRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Disciplina { Id = disciplinaId, Nome = "Disciplina" }]);
        _pesoDisciplinaService.Setup(s => s.ObterOuCalcularPesosAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PesoDisciplina(disciplinaId, 10m)]);

        // IA propoe (de forma incoerente) 500 minutos, muito acima do tempo disponivel do usuario (60).
        var jsonResposta = $$"""{"itens": [{"topicoId": "{{topicoId}}", "tempoAlocadoMin": 500}]}""";
        ConfigurarRespostaLlm(jsonResposta);

        var resultado = await _service.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, tempoDisponivel, NivelUsuario.Iniciante);

        resultado.Itens.Should().ContainSingle();
        resultado.Itens[0].TempoAlocadoMin.Should().Be(tempoDisponivel);
    }

    [TestMethod]
    public async Task ObterOuGerarPlanoAtualAsync_RespostaDaIaInvalida_UsaPlanoDeReservaComTodosOsTopicosReais()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var editalId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        var topicoId = Guid.NewGuid();

        ConfigurarSemPlanoExistente(usuarioId);
        _editalRepository.Setup(r => r.GetMaisRecentePorCarreiraAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Edital { Id = editalId, CarreiraId = carreiraId });
        _topicoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Topico { Id = topicoId, EditalId = editalId, DisciplinaId = disciplinaId, Nome = "Topico", Ordem = 1 }]);
        _disciplinaRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Disciplina { Id = disciplinaId, Nome = "Disciplina" }]);
        _pesoDisciplinaService.Setup(s => s.ObterOuCalcularPesosAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PesoDisciplina(disciplinaId, 10m)]);

        ConfigurarRespostaLlm("isso nao e um json valido");

        var resultado = await _service.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, 45, NivelUsuario.Iniciante);

        resultado.Itens.Should().ContainSingle(i => i.TopicoId == topicoId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ObterOuGerarPlanoAtualAsync_CarreiraSemEdital_LancaNotFound()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();

        ConfigurarSemPlanoExistente(usuarioId);
        _editalRepository.Setup(r => r.GetMaisRecentePorCarreiraAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Edital?)null);

        var acao = () => _service.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, 60, NivelUsuario.Iniciante);

        await acao.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task ObterOuGerarPlanoAtualAsync_ComMultiplosPlanos_RetornaSempreOMaisRecente()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var planoAntigo = new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, CarreiraId = carreiraId, CriadoEm = DateTimeOffset.UtcNow.AddDays(-10) };
        var planoRecente = new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, CarreiraId = carreiraId, CriadoEm = DateTimeOffset.UtcNow };

        // Ordem de retorno do repositorio embaralhada de proposito, pra confirmar que quem ordena e o servico.
        _planoEstudoRepository.Setup(r => r.GetByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([planoAntigo, planoRecente]);
        _planoItemRepository.Setup(r => r.GetByPlanoIdAsync(planoRecente.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var resultado = await _service.ObterOuGerarPlanoAtualAsync(usuarioId, carreiraId, 60, NivelUsuario.Iniciante);

        resultado.Plano.Should().BeSameAs(planoRecente);
    }

    [TestMethod]
    public async Task RecriarPlanoAsync_SemPlanoAnterior_GeraNovoSemResumo()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var editalId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        var topicoId = Guid.NewGuid();

        ConfigurarSemPlanoExistente(usuarioId);
        _editalRepository.Setup(r => r.GetMaisRecentePorCarreiraAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Edital { Id = editalId, CarreiraId = carreiraId });
        _topicoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Topico { Id = topicoId, EditalId = editalId, DisciplinaId = disciplinaId, Nome = "Topico", Ordem = 1 }]);
        _disciplinaRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Disciplina { Id = disciplinaId, Nome = "Disciplina" }]);
        _pesoDisciplinaService.Setup(s => s.ObterOuCalcularPesosAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PesoDisciplina(disciplinaId, 10m)]);
        ConfigurarRespostaLlm($$"""{"itens": [{"topicoId": "{{topicoId}}", "tempoAlocadoMin": 30}]}""");

        var resultado = await _service.RecriarPlanoAsync(usuarioId, carreiraId, 60, NivelUsuario.Iniciante);

        resultado.ResumoAnterior.Should().BeNull();
        resultado.PlanoNovo.Itens.Should().ContainSingle(i => i.TopicoId == topicoId);
    }

    [TestMethod]
    public async Task RecriarPlanoAsync_ComPlanoAnterior_GeraNovoEMantemOAnteriorNoBanco()
    {
        var usuarioId = Guid.NewGuid();
        var carreiraId = Guid.NewGuid();
        var editalId = Guid.NewGuid();
        var disciplinaId = Guid.NewGuid();
        var topicoId = Guid.NewGuid();
        var planoAnterior = new PlanoEstudo { Id = Guid.NewGuid(), UsuarioId = usuarioId, CarreiraId = carreiraId, CriadoEm = DateTimeOffset.UtcNow.AddDays(-5) };
        var itensAnteriores = new List<PlanoItem>
        {
            new() { Id = Guid.NewGuid(), PlanoId = planoAnterior.Id, TopicoId = Guid.NewGuid(), Status = StatusItemPlano.Concluido },
            new() { Id = Guid.NewGuid(), PlanoId = planoAnterior.Id, TopicoId = Guid.NewGuid(), Status = StatusItemPlano.Concluido },
            new() { Id = Guid.NewGuid(), PlanoId = planoAnterior.Id, TopicoId = Guid.NewGuid(), Status = StatusItemPlano.Pendente },
        };

        _planoEstudoRepository.Setup(r => r.GetByUsuarioIdAsync(usuarioId, It.IsAny<CancellationToken>())).ReturnsAsync([planoAnterior]);
        _planoItemRepository.Setup(r => r.GetByPlanoIdAsync(planoAnterior.Id, It.IsAny<CancellationToken>())).ReturnsAsync(itensAnteriores);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        _editalRepository.Setup(r => r.GetMaisRecentePorCarreiraAsync(carreiraId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Edital { Id = editalId, CarreiraId = carreiraId, DataProva = hoje.AddDays(20) });
        _topicoRepository.Setup(r => r.GetByEditalIdAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Topico { Id = topicoId, EditalId = editalId, DisciplinaId = disciplinaId, Nome = "Topico", Ordem = 1 }]);
        _disciplinaRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Disciplina { Id = disciplinaId, Nome = "Disciplina" }]);
        _pesoDisciplinaService.Setup(s => s.ObterOuCalcularPesosAsync(editalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PesoDisciplina(disciplinaId, 10m)]);
        ConfigurarRespostaLlm($$"""{"itens": [{"topicoId": "{{topicoId}}", "tempoAlocadoMin": 30}]}""");

        var resultado = await _service.RecriarPlanoAsync(usuarioId, carreiraId, 60, NivelUsuario.Iniciante);

        resultado.ResumoAnterior.Should().NotBeNull();
        resultado.ResumoAnterior!.TotalTopicos.Should().Be(3);
        resultado.ResumoAnterior.TopicosConcluidos.Should().Be(2);
        resultado.ResumoAnterior.DiasRestantesAteProva.Should().Be(20);

        resultado.PlanoNovo.Plano.Id.Should().NotBe(planoAnterior.Id, "o plano novo tem que ser uma entidade diferente do anterior");

        // O plano anterior nunca é removido — só deixa de ser "o mais recente". IRepository não expõe
        // mais um método de remoção genérico, então essa garantia agora é estrutural, não testável via mock.
        _planoEstudoRepository.Verify(r => r.AddAsync(It.Is<PlanoEstudo>(p => p.Id == resultado.PlanoNovo.Plano.Id), It.IsAny<CancellationToken>()), Times.Once);
    }
}
