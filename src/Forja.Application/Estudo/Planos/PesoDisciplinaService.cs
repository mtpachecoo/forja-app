using System.Text.Json.Serialization;
using Forja.Application.Common;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Conteudo;
using Forja.Domain.Questoes;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IPesoDisciplinaService"/>.
/// </summary>
/// <remarks>
/// Cascata de cálculo, executada na primeira vez que um edital é referenciado (nunca recalculada
/// automaticamente nesta fase, mesmo para a fonte <see cref="FontePesoDisciplina.ContagemQuestoes"/>,
/// marcada como recalculável para um futuro job de reprocessamento periódico — não construído aqui):
/// <list type="number">
/// <item>Extração via IA de uma distribuição quantitativa declarada no próprio texto do edital.</item>
/// <item>Contagem de questões aprovadas por disciplina, com piso de amostra mínima.</item>
/// <item>Herança dos pesos do edital anterior mais recente da mesma carreira.</item>
/// <item>Peso igual entre as disciplinas do edital — nunca persistido (não corresponde a nenhum dos
/// três valores de <see cref="FontePesoDisciplina"/>; é recomputado a cada chamada, o que é barato
/// porque não envolve IA nem agregação).</item>
/// </list>
/// </remarks>
public class PesoDisciplinaService : IPesoDisciplinaService
{
    /// <summary>Quantidade mínima de questões aprovadas para uma disciplina usar a contagem real como peso.</summary>
    private const int AmostraMinima = 10;

    private readonly IEditalPesoDisciplinaRepository _pesoRepository;
    private readonly IEditalRepository _editalRepository;
    private readonly ITopicoRepository _topicoRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;
    private readonly IChunkConteudoRepository _chunkRepository;
    private readonly IQuestaoRepository _questaoRepository;
    private readonly IGeradorDeRespostaChat _geradorDeRespostaChat;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public PesoDisciplinaService(
        IEditalPesoDisciplinaRepository pesoRepository,
        IEditalRepository editalRepository,
        ITopicoRepository topicoRepository,
        IDisciplinaRepository disciplinaRepository,
        IChunkConteudoRepository chunkRepository,
        IQuestaoRepository questaoRepository,
        IGeradorDeRespostaChat geradorDeRespostaChat,
        IUnitOfWork unitOfWork)
    {
        _pesoRepository = pesoRepository;
        _editalRepository = editalRepository;
        _topicoRepository = topicoRepository;
        _disciplinaRepository = disciplinaRepository;
        _chunkRepository = chunkRepository;
        _questaoRepository = questaoRepository;
        _geradorDeRespostaChat = geradorDeRespostaChat;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PesoDisciplina>> ObterOuCalcularPesosAsync(Guid editalId, CancellationToken cancellationToken = default)
    {
        var existentes = await _pesoRepository.GetByEditalIdAsync(editalId, cancellationToken);
        if (existentes.Count > 0)
        {
            return existentes.Select(p => new PesoDisciplina(p.DisciplinaId, p.Peso)).ToList();
        }

        var edital = await _editalRepository.GetByIdAsync(editalId, cancellationToken)
            ?? throw new NotFoundException("Edital", editalId);

        var topicos = await _topicoRepository.GetByEditalIdAsync(editalId, cancellationToken);
        var disciplinaIds = topicos.Select(t => t.DisciplinaId).Distinct().ToList();

        // Caso (a): extração via IA de uma distribuição quantitativa declarada no próprio edital.
        var extraido = await TentarExtrairViaIaAsync(editalId, disciplinaIds, cancellationToken);
        if (extraido is { Count: > 0 })
        {
            return await PersistirAsync(editalId, extraido, FontePesoDisciplina.ExtraidoEdital, editalOrigemId: null, cancellationToken);
        }

        // Caso (b): contagem de questões aprovadas por disciplina, com piso de amostra mínima.
        var contagens = await _questaoRepository.ContarAprovadasPorDisciplinaAsync(edital.CarreiraId, cancellationToken);
        if (contagens.Values.Sum() > 0)
        {
            var pesosPorContagem = disciplinaIds
                .Select(id =>
                {
                    var quantidade = contagens.GetValueOrDefault(id, 0);
                    var peso = quantidade >= AmostraMinima ? (decimal)quantidade : (decimal)AmostraMinima;
                    return new PesoDisciplina(id, peso);
                })
                .ToList();

            return await PersistirAsync(editalId, pesosPorContagem, FontePesoDisciplina.ContagemQuestoes, editalOrigemId: null, cancellationToken);
        }

        // Caso (c): herdar os pesos do edital anterior mais recente da mesma carreira, se houver.
        var pesosHerdados = await _pesoRepository.ObterPesosDoEditalAnteriorMaisRecenteAsync(edital.CarreiraId, editalId, cancellationToken);
        if (pesosHerdados.Count > 0)
        {
            var editalOrigemId = pesosHerdados[0].EditalId;
            var pesos = pesosHerdados.Select(p => new PesoDisciplina(p.DisciplinaId, p.Peso)).ToList();
            return await PersistirAsync(editalId, pesos, FontePesoDisciplina.HerdadoEditalAnterior, editalOrigemId, cancellationToken);
        }

        // Caso (d): carreira nova, sem nenhum histórico — peso igual entre as disciplinas do edital.
        // Não persistido: recomputar isso a cada chamada é praticamente grátis (nenhuma consulta além
        // das já feitas acima) e a "fonte" não tem um valor correspondente no enum.
        return disciplinaIds.Select(id => new PesoDisciplina(id, 1m)).ToList();
    }

    private async Task<IReadOnlyList<PesoDisciplina>> PersistirAsync(
        Guid editalId,
        IReadOnlyList<PesoDisciplina> pesos,
        FontePesoDisciplina fonte,
        Guid? editalOrigemId,
        CancellationToken cancellationToken)
    {
        var agora = DateTimeOffset.UtcNow;
        var entidades = pesos.Select(p => new EditalPesoDisciplina
        {
            EditalId = editalId,
            DisciplinaId = p.DisciplinaId,
            Peso = p.Peso,
            Fonte = fonte,
            EditalOrigemId = editalOrigemId,
            CalculadoEm = agora,
        }).ToList();

        await _pesoRepository.AddRangeAsync(entidades, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return pesos;
    }

    private async Task<IReadOnlyList<PesoDisciplina>?> TentarExtrairViaIaAsync(
        Guid editalId,
        IReadOnlyList<Guid> disciplinaIds,
        CancellationToken cancellationToken)
    {
        if (disciplinaIds.Count == 0)
        {
            return null;
        }

        var chunks = await _chunkRepository.GetByEditalIdAsync(editalId, cancellationToken);
        if (chunks.Count == 0)
        {
            return null;
        }

        var disciplinas = await _disciplinaRepository.GetByIdsAsync(disciplinaIds, cancellationToken);
        if (disciplinas.Count == 0)
        {
            return null;
        }

        var textoEdital = string.Join("\n\n", chunks.Select(c => c.Texto));
        var nomesDisciplinas = string.Join(", ", disciplinas.Select(d => d.Nome));

        var prompt = """
            Analise o texto de edital de concurso público abaixo. Verifique se ele contém uma
            DISTRIBUIÇÃO QUANTITATIVA de questões por disciplina (uma tabela ou lista explícita
            dizendo quantas questões cada disciplina terá na prova).

            Disciplinas conhecidas neste edital: __NOMES_DISCIPLINAS__

            Texto do edital:
            __TEXTO_EDITAL__

            Responda ESTRITAMENTE em JSON, sem nenhum texto adicional, comentário ou markdown, no formato:
            {"encontrado": true, "disciplinas": [{"nome": "<nome exatamente como na lista acima>", "peso": <quantidade de questões>}]}
            ou, se não encontrar uma distribuição quantitativa explícita:
            {"encontrado": false, "disciplinas": []}
            """
            .Replace("__NOMES_DISCIPLINAS__", nomesDisciplinas)
            .Replace("__TEXTO_EDITAL__", textoEdital);

        var extracao = await _geradorDeRespostaChat.PedirRespostaEstruturadaAsync<ExtracaoPesoResponse>(prompt, cancellationToken: cancellationToken);
        if (extracao is not { Encontrado: true, Disciplinas.Count: > 0 })
        {
            return null;
        }

        var idPorNome = disciplinas.ToDictionary(d => d.Nome.Trim(), d => d.Id, StringComparer.OrdinalIgnoreCase);

        var pesos = new List<PesoDisciplina>();
        foreach (var item in extracao.Disciplinas)
        {
            // Grounding: qualquer disciplina que a IA mencione fora do catálogo real do edital é descartada.
            if (idPorNome.TryGetValue(item.Nome.Trim(), out var disciplinaId))
            {
                pesos.Add(new PesoDisciplina(disciplinaId, item.Peso));
            }
        }

        return pesos.Count > 0 ? pesos : null;
    }

    private sealed record ExtracaoPesoResponse(
        [property: JsonPropertyName("encontrado")] bool Encontrado,
        [property: JsonPropertyName("disciplinas")] List<ExtracaoPesoDisciplina> Disciplinas);

    private sealed record ExtracaoPesoDisciplina(
        [property: JsonPropertyName("nome")] string Nome,
        [property: JsonPropertyName("peso")] decimal Peso);
}
