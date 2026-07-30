using Forja.Domain.Common;

namespace Forja.Domain.Gamificacao;

/// <summary>
/// Contrato de repositório para a entidade <see cref="Pontuacao"/>.
/// </summary>
public interface IPontuacaoRepository : IRepository<Pontuacao, Guid>
{
    /// <summary>
    /// Obtém as pontuações da semana informada, ordenadas por <see cref="Pontuacao.PontosSemanaAtual"/>
    /// decrescente — usado para montar o ranking semanal (RF-013).
    /// </summary>
    /// <param name="semanaReferencia">Data de início da semana de referência.</param>
    /// <param name="usuarioIds">
    /// Quando informado, restringe o ranking a esses usuários (ex.: usuários de uma carreira específica).
    /// A restrição é aplicada antes da paginação, para que <paramref name="skip"/>/<paramref name="take"/>
    /// naveguem corretamente pelo conjunto já filtrado. <c>null</c> para o ranking geral.
    /// </param>
    /// <param name="skip">Quantidade de posições a pular (paginação).</param>
    /// <param name="take">Quantidade máxima de posições a retornar (paginação).</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das pontuações da semana, da maior para a menor, já paginada.</returns>
    Task<IReadOnlyList<Pontuacao>> GetRankingSemanalAsync(
        DateOnly semanaReferencia,
        IReadOnlyCollection<Guid>? usuarioIds,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Incrementa a pontuação do usuário atomicamente no próprio Postgres (upsert
    /// <c>INSERT ... ON CONFLICT DO UPDATE</c>), sem passar por ler→somar em memória→salvar.
    /// Evita a condição de corrida clássica de "lost update": duas chamadas concorrentes pro mesmo
    /// usuário (ex.: respostas quase simultâneas) não podem mais se sobrescrever, porque a soma
    /// acontece dentro da própria instrução SQL, não em memória entre uma leitura e um save separados.
    /// Aplica a mesma regra de <see cref="Pontuacao.RegistrarPontos"/> (acumula na semana atual, ou
    /// reinicia <see cref="Pontuacao.PontosSemanaAtual"/> se a semana mudou).
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="pontos">Pontos a adicionar.</param>
    /// <param name="hoje">Data em que os pontos foram ganhos.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A pontuação já atualizada, refletindo o estado gravado no banco.</returns>
    Task<Pontuacao> IncrementarPontosAsync(Guid usuarioId, int pontos, DateOnly hoje, CancellationToken cancellationToken = default);
}
