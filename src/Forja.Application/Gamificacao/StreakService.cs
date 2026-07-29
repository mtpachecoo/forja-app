using Forja.Domain.Gamificacao;

namespace Forja.Application.Gamificacao;

/// <summary>
/// Implementação padrão de <see cref="IStreakService"/>.
/// </summary>
public class StreakService : IStreakService
{
    private readonly IStreakRepository _streakRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public StreakService(IStreakRepository streakRepository)
    {
        _streakRepository = streakRepository;
    }

    /// <inheritdoc />
    public async Task<Streak> RegistrarAtividadeAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var existente = await _streakRepository.GetByIdAsync(usuarioId, cancellationToken);

        if (existente is null)
        {
            var novo = new Streak { UsuarioId = usuarioId, DiasConsecutivos = 1, UltimaAtividadeEm = hoje };
            await _streakRepository.AddAsync(novo, cancellationToken);
            return novo;
        }

        if (existente.UltimaAtividadeEm == hoje)
        {
            // Atividade de hoje já registrada — sem mudança.
            return existente;
        }

        existente.DiasConsecutivos = existente.UltimaAtividadeEm == hoje.AddDays(-1)
            ? existente.DiasConsecutivos + 1
            : 1;
        existente.UltimaAtividadeEm = hoje;

        _streakRepository.Update(existente);

        // Quem persiste (IUnitOfWork.SaveChangesAsync) é o orquestrador
        // (IniciarSessaoComEfeitosService), numa única transação com a sessão.
        return existente;
    }
}
