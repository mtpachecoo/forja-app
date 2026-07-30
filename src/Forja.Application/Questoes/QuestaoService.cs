using Forja.Application.Common;
using Forja.Domain.Questoes;

namespace Forja.Application.Questoes;

/// <summary>
/// Implementação padrão de <see cref="IQuestaoService"/>.
/// </summary>
public class QuestaoService : IQuestaoService
{
    /// <summary>Teto de itens por página, independente do que for pedido — evita varreduras não paginadas na prática.</summary>
    private const int TamanhoPaginaMaximo = 100;

    private readonly IQuestaoRepository _questaoRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    /// <param name="questaoRepository">Repositório de questões.</param>
    public QuestaoService(IQuestaoRepository questaoRepository)
    {
        _questaoRepository = questaoRepository;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Questao>> BuscarAsync(
        Guid? carreiraId,
        Guid? bancaId,
        Guid? disciplinaId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var tamanho = Math.Clamp(take, 1, TamanhoPaginaMaximo);
        var deslocamento = Math.Max(skip, 0);
        return _questaoRepository.GetByFiltroAsync(carreiraId, bancaId, disciplinaId, StatusQuestao.Aprovada, deslocamento, tamanho, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Questao> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var questao = await _questaoRepository.GetByIdAsync(id, cancellationToken);
        if (questao is null || questao.Status != StatusQuestao.Aprovada)
        {
            throw new NotFoundException("Questão", id);
        }

        return questao;
    }
}
