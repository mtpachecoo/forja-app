namespace Forja.Application.Duvidas;

/// <summary>
/// Serviço de esclarecimento de dúvidas contextuais sobre uma questão (RF-014), usando RAG
/// (Retrieval-Augmented Generation) sobre o conteúdo já indexado em <c>chunks_conteudo</c>.
/// </summary>
public interface IRagService
{
    /// <summary>
    /// Responde a uma dúvida do aluno sobre uma questão, com base nos trechos de conteúdo mais
    /// similares semanticamente à pergunta.
    /// </summary>
    /// <param name="questaoId">Identificador da questão à qual a dúvida se refere.</param>
    /// <param name="pergunta">Pergunta do aluno.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A resposta gerada e os chunks usados como contexto.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">
    /// Lançada quando a questão não existe ou não está aprovada.
    /// </exception>
    Task<RespostaDuvida> ResponderDuvidaAsync(Guid questaoId, string pergunta, CancellationToken cancellationToken = default);
}
