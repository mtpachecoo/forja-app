using Forja.Domain.Catalogo;
using Forja.Domain.Estudo;

namespace Forja.Application.Desempenho;

/// <summary>
/// Implementação padrão de <see cref="IAnaliseDesempenhoService"/>.
/// </summary>
/// <remarks>
/// Alerta gerado por template fixo, não por IA: a detecção já é 100% determinística (contagem/média
/// sobre <c>respostas_usuario</c>) e a mensagem só precisa comunicar dois números e uma disciplina —
/// não há necessidade de geração livre de texto nem de grounding em conteúdo externo (diferente do
/// RAG de dúvidas ou da geração do plano de estudo, onde IA agrega valor real). Usar IA aqui só
/// adicionaria latência, custo e não-determinismo sem ganho, além de tornar o teste do alerta
/// dependente de mock de LLM em vez de aritmética simples.
/// </remarks>
public class AnaliseDesempenhoService : IAnaliseDesempenhoService
{
    /// <summary>Tamanho de cada janela de respostas comparada (anterior vs. recente).</summary>
    private const int TamanhoJanela = 10;

    /// <summary>Amostra mínima em cada janela para considerar a comparação confiável.</summary>
    private const int AmostraMinimaPorJanela = 5;

    /// <summary>Queda mínima (pontos percentuais) para considerar uma queda real, não ruído.</summary>
    private const decimal QuedaMinimaPontosPercentuais = 15m;

    private readonly IRespostaUsuarioRepository _respostaRepository;
    private readonly IDisciplinaRepository _disciplinaRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public AnaliseDesempenhoService(IRespostaUsuarioRepository respostaRepository, IDisciplinaRepository disciplinaRepository)
    {
        _respostaRepository = respostaRepository;
        _disciplinaRepository = disciplinaRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AlertaDesempenho>> DetectarQuedaDeDesempenhoAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var historico = await _respostaRepository.GetHistoricoPorDisciplinaAsync(usuarioId, cancellationToken);

        var quedas = new List<(Guid DisciplinaId, decimal TaxaAnterior, decimal TaxaRecente)>();
        foreach (var grupo in historico.GroupBy(r => r.DisciplinaId))
        {
            var respostas = grupo.OrderBy(r => r.CriadoEm).ToList();
            if (respostas.Count < TamanhoJanela * 2)
            {
                continue; // amostra insuficiente pra formar as duas janelas
            }

            var recentes = respostas.TakeLast(TamanhoJanela).ToList();
            var anteriores = respostas.Skip(respostas.Count - (TamanhoJanela * 2)).Take(TamanhoJanela).ToList();

            if (recentes.Count < AmostraMinimaPorJanela || anteriores.Count < AmostraMinimaPorJanela)
            {
                continue;
            }

            var taxaRecente = TaxaDeAcerto(recentes);
            var taxaAnterior = TaxaDeAcerto(anteriores);

            if (taxaAnterior - taxaRecente >= QuedaMinimaPontosPercentuais)
            {
                quedas.Add((grupo.Key, taxaAnterior, taxaRecente));
            }
        }

        if (quedas.Count == 0)
        {
            return [];
        }

        var disciplinas = await _disciplinaRepository.GetByIdsAsync(quedas.Select(q => q.DisciplinaId), cancellationToken);
        var nomePorDisciplinaId = disciplinas.ToDictionary(d => d.Id, d => d.Nome);

        return quedas
            .Select(q => new AlertaDesempenho(
                q.DisciplinaId,
                q.TaxaAnterior,
                q.TaxaRecente,
                MontarMensagem(nomePorDisciplinaId.GetValueOrDefault(q.DisciplinaId, "essa disciplina"), q.TaxaAnterior, q.TaxaRecente)))
            .ToList();
    }

    private static decimal TaxaDeAcerto(IReadOnlyCollection<RespostaDisciplina> respostas)
        => (decimal)respostas.Count(r => r.Correta) / respostas.Count * 100m;

    private static string MontarMensagem(string nomeDisciplina, decimal taxaAnterior, decimal taxaRecente)
        => $"Sua taxa de acerto em {nomeDisciplina} caiu de {taxaAnterior:F0}% para {taxaRecente:F0}% nas últimas questões. Vale revisar o conteúdo antes de seguir em frente.";
}
