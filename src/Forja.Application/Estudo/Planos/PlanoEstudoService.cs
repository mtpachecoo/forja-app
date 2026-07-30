using System.Text.Json.Serialization;
using Forja.Application.Common;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Usuarios;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IPlanoEstudoService"/>.
/// </summary>
public class PlanoEstudoService : IPlanoEstudoService
{
    private readonly IPlanoEstudoRepository _planoEstudoRepository;
    private readonly IPlanoItemRepository _planoItemRepository;
    private readonly IEditalRepository _editalRepository;
    private readonly ITopicoRepository _topicoRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IPesoDisciplinaService _pesoDisciplinaService;
    private readonly IGeradorDeRespostaChat _geradorDeRespostaChat;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public PlanoEstudoService(
        IPlanoEstudoRepository planoEstudoRepository,
        IPlanoItemRepository planoItemRepository,
        IEditalRepository editalRepository,
        ITopicoRepository topicoRepository,
        IDisciplinaRepository disciplinaRepository,
        IPesoDisciplinaService pesoDisciplinaService,
        IGeradorDeRespostaChat geradorDeRespostaChat,
        IUnitOfWork unitOfWork)
    {
        _planoEstudoRepository = planoEstudoRepository;
        _planoItemRepository = planoItemRepository;
        _editalRepository = editalRepository;
        _topicoRepository = topicoRepository;
        _disciplinaRepository = disciplinaRepository;
        _pesoDisciplinaService = pesoDisciplinaService;
        _geradorDeRespostaChat = geradorDeRespostaChat;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<PlanoGerado> ObterOuGerarPlanoAtualAsync(
        Guid usuarioId,
        Guid carreiraId,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken = default)
    {
        var planoExistente = await ObterPlanoMaisRecenteAsync(usuarioId, carreiraId, cancellationToken);
        if (planoExistente is not null)
        {
            var itensExistentes = await _planoItemRepository.GetByPlanoIdAsync(planoExistente.Id, cancellationToken);
            return new PlanoGerado(planoExistente, itensExistentes);
        }

        return await GerarNovoPlanoAsync(usuarioId, carreiraId, tempoDisponivelMinDia, nivel, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecriarPlanoResultado> RecriarPlanoAsync(
        Guid usuarioId,
        Guid carreiraId,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken = default)
    {
        // Resumo do que estava ativo, "anotado" antes de ser substituído — o plano em si não é
        // apagado, só deixa de ser o mais recente.
        var resumoAnterior = await ObterResumoDoPlanoAnteriorAsync(usuarioId, carreiraId, cancellationToken);
        var planoNovo = await GerarNovoPlanoAsync(usuarioId, carreiraId, tempoDisponivelMinDia, nivel, cancellationToken);

        return new RecriarPlanoResultado(resumoAnterior, planoNovo);
    }

    private async Task<PlanoEstudo?> ObterPlanoMaisRecenteAsync(Guid usuarioId, Guid carreiraId, CancellationToken cancellationToken)
    {
        var planosDoUsuario = await _planoEstudoRepository.GetByUsuarioIdAsync(usuarioId, cancellationToken);
        return planosDoUsuario
            .Where(p => p.CarreiraId == carreiraId)
            .OrderByDescending(p => p.CriadoEm)
            .FirstOrDefault();
    }

    private async Task<ResumoPlanoAnterior?> ObterResumoDoPlanoAnteriorAsync(Guid usuarioId, Guid carreiraId, CancellationToken cancellationToken)
    {
        var planoAnterior = await ObterPlanoMaisRecenteAsync(usuarioId, carreiraId, cancellationToken);
        if (planoAnterior is null)
        {
            return null;
        }

        var itens = await _planoItemRepository.GetByPlanoIdAsync(planoAnterior.Id, cancellationToken);
        var edital = await _editalRepository.GetMaisRecentePorCarreiraAsync(carreiraId, cancellationToken);

        int? diasRestantes = edital?.DataProva is { } dataProva
            ? Math.Max(0, (dataProva.ToDateTime(TimeOnly.MinValue) - DateTimeOffset.UtcNow.UtcDateTime.Date).Days)
            : null;

        return new ResumoPlanoAnterior(
            itens.Count,
            itens.Count(i => i.Status == StatusItemPlano.Concluido),
            diasRestantes);
    }

    private async Task<PlanoGerado> GerarNovoPlanoAsync(
        Guid usuarioId,
        Guid carreiraId,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken)
    {
        var edital = await _editalRepository.GetMaisRecentePorCarreiraAsync(carreiraId, cancellationToken)
            ?? throw new NotFoundException("Edital para a carreira", carreiraId);

        var topicos = await _topicoRepository.GetByEditalIdAsync(edital.Id, cancellationToken);
        if (topicos.Count == 0)
        {
            throw new NotFoundException("Tópicos do edital", edital.Id);
        }

        var disciplinaIds = topicos.Select(t => t.DisciplinaId).Distinct().ToList();
        var disciplinas = await _disciplinaRepository.GetByIdsAsync(disciplinaIds, cancellationToken);
        var nomeDisciplinaPorId = disciplinas.ToDictionary(d => d.Id, d => d.Nome);

        var pesos = await _pesoDisciplinaService.ObterOuCalcularPesosAsync(edital.Id, cancellationToken);
        var pesoPorDisciplina = pesos.ToDictionary(p => p.DisciplinaId, p => p.Peso);

        var alocacoes = await ObterAlocacaoViaIaAsync(topicos, nomeDisciplinaPorId, pesoPorDisciplina, edital.DataProva, tempoDisponivelMinDia, nivel, cancellationToken);

        var topicoIdsValidos = topicos.Select(t => t.Id).ToHashSet();
        var alocacoesValidas = alocacoes
            // Grounding: nunca inclui um tópico fora do catálogo real do edital, mesmo que a IA alucine um.
            .Where(a => topicoIdsValidos.Contains(a.TopicoId))
            .ToList();

        if (alocacoesValidas.Count == 0)
        {
            // Não foi possível obter uma alocação válida da IA — plano determinístico de reserva,
            // priorizando disciplinas de maior peso, garantindo que o plano nunca fique vazio.
            alocacoesValidas = topicos
                .OrderByDescending(t => pesoPorDisciplina.GetValueOrDefault(t.DisciplinaId, 0m))
                .ThenBy(t => t.Ordem)
                .Select(t => (TopicoId: t.Id, TempoAlocadoMin: tempoDisponivelMinDia))
                .ToList();
        }

        var planoId = Guid.NewGuid();
        var itens = alocacoesValidas
            .Select((a, indice) => new PlanoItem
            {
                Id = Guid.NewGuid(),
                PlanoId = planoId,
                TopicoId = a.TopicoId,
                Ordem = indice + 1,
                // Coerência com o tempo disponível: nenhum item pode alocar mais minutos do que o
                // usuário tem por dia, nem um valor não positivo.
                TempoAlocadoMin = Math.Clamp(a.TempoAlocadoMin, 1, tempoDisponivelMinDia),
                Status = StatusItemPlano.Pendente,
            })
            .ToList();

        var plano = new PlanoEstudo
        {
            Id = planoId,
            UsuarioId = usuarioId,
            CarreiraId = carreiraId,
            GeradoViaIA = true,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await _planoEstudoRepository.AddAsync(plano, cancellationToken);
        foreach (var item in itens)
        {
            await _planoItemRepository.AddAsync(item, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlanoGerado(plano, itens);
    }

    private async Task<IReadOnlyList<(Guid TopicoId, int TempoAlocadoMin)>> ObterAlocacaoViaIaAsync(
        IReadOnlyList<Topico> topicos,
        IReadOnlyDictionary<Guid, string> nomeDisciplinaPorId,
        IReadOnlyDictionary<Guid, decimal> pesoPorDisciplina,
        DateOnly? dataProva,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken)
    {
        var linhasTopicos = string.Join("\n", topicos.Select(t =>
        {
            var nomeDisciplina = nomeDisciplinaPorId.GetValueOrDefault(t.DisciplinaId, "Desconhecida");
            var peso = pesoPorDisciplina.GetValueOrDefault(t.DisciplinaId, 0m);
            return $"{t.Id} | {nomeDisciplina} (peso {peso}) | {t.Nome}";
        }));

        var infoProva = dataProva.HasValue
            ? $"A prova está marcada para {dataProva:dd/MM/yyyy} (faltam {(dataProva.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).Days} dias) — priorize dar conta do conteúdo de maior peso com essa proximidade em mente."
            : "A data da prova ainda não foi divulgada — não há um fator de proximidade a considerar.";

        var prompt = """
            Monte um plano de estudo para um concurso público, escolhendo e ordenando por prioridade os
            tópicos abaixo (formato: id | disciplina (peso) | tópico). Use SOMENTE os IDs de tópico
            listados — não invente nenhum tópico ou disciplina fora desta lista.

            __LINHAS_TOPICOS__

            Nível do aluno: __NIVEL__
            Tempo disponível por dia de estudo: __TEMPO_DISPONIVEL__ minutos
            __INFO_PROVA__

            Priorize disciplinas de maior peso, mas varie a ordem para não repetir a mesma disciplina
            em sequência. Para cada tópico escolhido, aloque quantos minutos dedicar a ele numa sessão
            de estudo (nunca mais que o tempo disponível por dia).

            Responda ESTRITAMENTE em JSON, sem texto adicional, comentário ou markdown, no formato:
            {"itens": [{"topicoId": "<id exatamente como na lista acima>", "tempoAlocadoMin": <minutos>}]}
            """
            .Replace("__LINHAS_TOPICOS__", linhasTopicos)
            .Replace("__NIVEL__", nivel.ToString())
            .Replace("__TEMPO_DISPONIVEL__", tempoDisponivelMinDia.ToString())
            .Replace("__INFO_PROVA__", infoProva);

        var historico = new ChatHistory();
        historico.AddUserMessage(prompt);

        var respostas = await _chatCompletionService.GetChatMessageContentsAsync(historico, cancellationToken: cancellationToken);
        var textoResposta = respostas[0].Content;
        if (string.IsNullOrWhiteSpace(textoResposta))
        {
            return [];
        }

        var plano = IaJsonResponseParser.TentarDesserializar<PlanoResponse>(textoResposta);
        if (plano is null)
        {
            return [];
        }

        var resultado = new List<(Guid, int)>();
        foreach (var item in plano.Itens)
        {
            if (Guid.TryParse(item.TopicoId, out var topicoId))
            {
                resultado.Add((topicoId, item.TempoAlocadoMin));
            }
        }

        return resultado;
    }

    private sealed record PlanoResponse(
        [property: JsonPropertyName("itens")] List<PlanoResponseItem> Itens);

    private sealed record PlanoResponseItem(
        [property: JsonPropertyName("topicoId")] string TopicoId,
        [property: JsonPropertyName("tempoAlocadoMin")] int TempoAlocadoMin);
}
