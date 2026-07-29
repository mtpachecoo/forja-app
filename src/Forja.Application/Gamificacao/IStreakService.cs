using Forja.Domain.Gamificacao;

namespace Forja.Application.Gamificacao;

/// <summary>
/// Serviço de sequência de dias consecutivos de estudo (streak).
/// </summary>
public interface IStreakService
{
    /// <summary>
    /// Registra atividade do usuário no dia de hoje, incrementando a sequência se a última atividade
    /// foi ontem, reiniciando-a se houve uma lacuna, ou mantendo-a se hoje já foi registrado.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O streak atualizado.</returns>
    Task<Streak> RegistrarAtividadeAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
