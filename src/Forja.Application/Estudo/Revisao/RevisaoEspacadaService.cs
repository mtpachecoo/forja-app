using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IRevisaoEspacadaService"/>.
/// </summary>
public class RevisaoEspacadaService : IRevisaoEspacadaService
{
    /// <summary>Intervalo mínimo (dias) para o qual o intervalo reseta após um erro.</summary>
    private const int IntervaloMinimoDias = 1;

    private readonly IRevisaoEspacadaRepository _revisaoRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public RevisaoEspacadaService(IRevisaoEspacadaRepository revisaoRepository)
    {
        _revisaoRepository = revisaoRepository;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RevisaoEspacada>> ObterPendentesAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        return _revisaoRepository.GetPendentesAsync(usuarioId, hoje, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RevisaoEspacada> RegistrarRespostaAsync(Guid usuarioId, Guid questaoId, bool correta, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var existente = await _revisaoRepository.GetByUsuarioIdEQuestaoIdAsync(usuarioId, questaoId, cancellationToken);
        var jaExistia = existente is not null;
        var revisao = existente ?? new RevisaoEspacada
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            QuestaoId = questaoId,
            ErrosConsecutivos = 0,
            IntervaloDiasAtual = IntervaloMinimoDias,
        };

        // RN-003: acerto aumenta o intervalo (revisa mais tarde); erro consecutivo reseta o intervalo
        // para o mínimo (revisa de novo mais cedo) — não o contrário.
        if (correta)
        {
            revisao.ErrosConsecutivos = 0;
            revisao.IntervaloDiasAtual *= 2;
        }
        else
        {
            revisao.ErrosConsecutivos += 1;
            revisao.IntervaloDiasAtual = IntervaloMinimoDias;
        }

        revisao.ProximaRevisaoEm = hoje.AddDays(revisao.IntervaloDiasAtual);

        if (jaExistia)
        {
            _revisaoRepository.Update(revisao);
        }
        else
        {
            await _revisaoRepository.AddAsync(revisao, cancellationToken);
        }

        // Quem persiste (IUnitOfWork.SaveChangesAsync) é o orquestrador
        // (RegistrarRespostaComEfeitosService), numa única transação com a resposta.
        return revisao;
    }
}
