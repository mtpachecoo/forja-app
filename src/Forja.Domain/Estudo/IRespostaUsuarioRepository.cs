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

    /// <summary>
    /// Conta quantas respostas foram registradas durante um pomodoro. Usado por RN-011: o pomodoro só
    /// concede pontos se houve pelo menos uma resposta registrada durante o ciclo.
    /// </summary>
    /// <param name="pomodoroId">Identificador do pomodoro.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Quantidade de respostas registradas durante o pomodoro.</returns>
    Task<int> ContarRespostasNoPomodoroAsync(Guid pomodoroId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém, para cada disciplina que o usuário já respondeu, as últimas <paramref name="limitePorDisciplina"/>
    /// respostas (mais recentes primeiro dentro do limite, mas sem ordenação relativa entre disciplinas
    /// diferentes garantida). Usado pela análise de desempenho (RF-021) para detectar queda de taxa de
    /// acerto por disciplina, sem custo de IA. O limite é aplicado no próprio SQL (via janela
    /// <c>ROW_NUMBER() OVER (PARTITION BY disciplina_id ...)</c>) para nunca carregar o histórico
    /// inteiro do usuário em memória — só o suficiente para montar as janelas de comparação.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="limitePorDisciplina">Quantidade máxima de respostas mais recentes retornadas por disciplina.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura do histórico recente, limitado por disciplina.</returns>
    Task<IReadOnlyList<RespostaDisciplina>> GetHistoricoPorDisciplinaAsync(Guid usuarioId, int limitePorDisciplina, CancellationToken cancellationToken = default);
}

/// <summary>
/// Projeção enxuta de uma resposta de usuário com a disciplina da questão respondida — usada só para
/// a análise de desempenho (RF-021), sem carregar a entidade <see cref="RespostaUsuario"/> inteira.
/// </summary>
/// <param name="DisciplinaId">Identificador da disciplina da questão respondida.</param>
/// <param name="Correta">Indica se a resposta estava correta.</param>
/// <param name="CriadoEm">Data e hora em que a resposta foi registrada.</param>
public sealed record RespostaDisciplina(Guid DisciplinaId, bool Correta, DateTimeOffset CriadoEm);
