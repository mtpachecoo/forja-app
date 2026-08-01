using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IRevisaoEspacadaService"/>.
/// </summary>
public class RevisaoEspacadaService : IRevisaoEspacadaService
{
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
        };

        revisao.RegistrarResultado(correta, hoje);

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
