using Forja.Domain.Common;

namespace Forja.Domain.Gamificacao;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Pontuacao"/>.
/// </summary>
public interface IPontuacaoRepository : IRepository<Pontuacao, Guid>
{
}
