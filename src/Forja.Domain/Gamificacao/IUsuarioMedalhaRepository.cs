namespace Forja.Domain.Gamificacao;

/// <summary>
/// Contrato de repositório para a entidade <see cref="UsuarioMedalha"/>. Não estende
/// <see cref="Common.IRepository{TEntity,TKey}"/>: a chave primária é composta
/// (<see cref="UsuarioMedalha.UsuarioId"/> + <see cref="UsuarioMedalha.MedalhaId"/>), o que não cabe
/// no contrato de chave única do genérico.
/// </summary>
public interface IUsuarioMedalhaRepository
{
    /// <summary>
    /// Verifica se o usuário já possui a medalha informada.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="medalhaId">Identificador da medalha.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns><c>true</c> se o usuário já conquistou essa medalha.</returns>
    Task<bool> ExisteAsync(Guid usuarioId, Guid medalhaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Concede uma medalha a um usuário.
    /// </summary>
    /// <param name="usuarioMedalha">Registro de conquista a adicionar.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task AddAsync(UsuarioMedalha usuarioMedalha, CancellationToken cancellationToken = default);
}
