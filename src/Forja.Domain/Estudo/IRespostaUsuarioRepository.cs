using Forja.Domain.Common;

namespace Forja.Domain.Estudo;

/// <summary>
/// Contrato de repositório para a entidade <see cref="RespostaUsuario"/>.
/// </summary>
public interface IRespostaUsuarioRepository : IRepository<RespostaUsuario, Guid>
{
    /// <summary>
    /// Verifica se o usuário já possui, para a questão informada, alguma resposta que gerou pontuação.
    /// Usado para garantir que uma questão só pontua na primeira resposta correta.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="questaoId">Identificador da questão.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns><c>true</c> se já existe uma resposta pontuada do usuário para essa questão.</returns>
    Task<bool> ExisteRespostaPontuadaAsync(Guid usuarioId, Guid questaoId, CancellationToken cancellationToken = default);
}
