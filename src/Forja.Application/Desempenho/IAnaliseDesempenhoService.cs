namespace Forja.Application.Desempenho;

/// <summary>
/// Serviço de análise proativa de desempenho (RF-021): detecta queda na taxa de acerto por disciplina
/// comparando duas janelas de respostas recentes, via consulta SQL — sem custo de IA.
/// </summary>
public interface IAnaliseDesempenhoService
{
    /// <summary>
    /// Detecta disciplinas em que a taxa de acerto caiu de forma relevante entre a janela de respostas
    /// anterior e a mais recente.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura dos alertas de queda de desempenho (vazia se nenhuma disciplina caiu).</returns>
    Task<IReadOnlyList<AlertaDesempenho>> DetectarQuedaDeDesempenhoAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
