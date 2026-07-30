namespace Forja.Application.Home;

/// <summary>
/// Serviço de aplicação que agrega os dados da tela inicial — streak, pontuação, progresso do plano
/// de estudo atual e atividade recente — numa única resposta pensada pro frontend, sem exigir 4-5
/// chamadas separadas.
/// </summary>
public interface IHomeService
{
    /// <summary>
    /// Monta o resumo da Home para o usuário informado.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="quantidadeAtividades">Quantidade máxima de atividades recentes a incluir.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>
    /// Resumo agregado. Nunca lança por ausência de dados — usuário sem streak/pontuação/plano ainda
    /// recebe os valores zerados/nulos correspondentes; a Home só agrega o que já existe, nunca gera
    /// um plano novo (isso é responsabilidade do onboarding/RF-002).
    /// </returns>
    Task<ResumoHome> ObterResumoAsync(Guid usuarioId, int quantidadeAtividades, CancellationToken cancellationToken = default);
}

/// <summary>Resumo agregado da tela inicial.</summary>
/// <param name="DiasConsecutivos">Sequência atual de dias consecutivos de estudo (0 se nunca estudou).</param>
/// <param name="PontosTotal">Pontuação total acumulada (0 se nunca pontuou).</param>
/// <param name="PontosSemanaAtual">Pontuação acumulada na semana de referência atual.</param>
/// <param name="PercentualPlanoConcluido">
/// Percentual de itens concluídos do plano de estudo mais recente do usuário (entre todas as
/// carreiras), ou <c>null</c> se o usuário ainda não tem nenhum plano gerado.
/// </param>
/// <param name="RespostasHoje">Quantidade de respostas registradas hoje (calendário UTC).</param>
/// <param name="AtividadesRecentes">
/// Últimas respostas do usuário, mais recentes primeiro — os únicos eventos granulares com timestamp
/// já existentes na plataforma (streak/pontuação são snapshots de uma linha só, não uma série).
/// </param>
/// <remarks>
/// Não inclui "meta diária de questões": não há histórico de tempo médio por questão nem RN
/// documentada pra derivar isso de <c>TempoDisponivelMinDia</c> sem chutar uma constante — fica como
/// pendência pra quando existir telemetria de tempo de resposta suficiente pra calibrar.
/// </remarks>
public sealed record ResumoHome(
    int DiasConsecutivos,
    int PontosTotal,
    int PontosSemanaAtual,
    decimal? PercentualPlanoConcluido,
    int RespostasHoje,
    IReadOnlyList<AtividadeRecente> AtividadesRecentes);

/// <summary>Uma atividade recente do usuário (resposta a uma questão).</summary>
/// <param name="QuestaoId">Identificador da questão respondida.</param>
/// <param name="Correta">Indica se a resposta estava correta.</param>
/// <param name="CriadoEm">Data e hora em que a resposta foi registrada.</param>
public sealed record AtividadeRecente(Guid QuestaoId, bool Correta, DateTimeOffset CriadoEm);
