namespace Forja.Api;

/// <summary>
/// Corpo da requisição de <c>POST /duvidas</c>.
/// </summary>
/// <param name="QuestaoId">Identificador da questão à qual a dúvida se refere.</param>
/// <param name="Pergunta">Pergunta do aluno.</param>
public sealed record DuvidaRequest(Guid QuestaoId, string Pergunta);
