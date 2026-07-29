using Forja.Domain.Common;

namespace Forja.Domain.Estudo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Pomodoro"/>.
/// </summary>
public interface IPomodoroRepository : IRepository<Pomodoro, Guid>
{
}
