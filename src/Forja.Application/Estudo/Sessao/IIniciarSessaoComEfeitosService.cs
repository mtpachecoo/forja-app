using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Orquestra o início de uma sessão de estudo e os efeitos colaterais que dependem dele — hoje, o
/// registro de atividade no streak do usuário. Existe para que <c>Program.cs</c> não precise encadear
/// múltiplos serviços diretamente no endpoint <c>POST /sessao/iniciar</c>.
/// </summary>
public interface IIniciarSessaoComEfeitosService
{
    /// <summary>
    /// Inicia a sessão via <see cref="ISessaoEstudoService"/> e, em seguida, registra a atividade do
    /// dia no streak do usuário via <see cref="Forja.Application.Gamificacao.IStreakService"/>.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A sessão de estudo do dia (existente ou recém-criada).</returns>
    Task<SessaoEstudo> IniciarAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
