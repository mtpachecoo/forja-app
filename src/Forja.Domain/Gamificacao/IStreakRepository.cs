using Forja.Domain.Common;

namespace Forja.Domain.Gamificacao;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Streak"/>.
/// </summary>
public interface IStreakRepository : IRepository<Streak, Guid>
{
}
