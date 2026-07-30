using Forja.Domain.Common;

namespace Forja.Domain.Gamificacao;

/// <summary>
/// Contrato de repositório para a entidade <see cref="ReputacaoContribuicao"/>. Chave é
/// <see cref="ReputacaoContribuicao.UsuarioId"/>, mesmo formato de <see cref="IPontuacaoRepository"/>/
/// <see cref="IStreakRepository"/>.
/// </summary>
public interface IReputacaoContribuicaoRepository : IRepository<ReputacaoContribuicao, Guid>
{
}
