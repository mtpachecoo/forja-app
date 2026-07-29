namespace Forja.Application.Duvidas;

/// <summary>
/// Resultado do esclarecimento de uma dúvida contextual (RF-014).
/// </summary>
/// <param name="Resposta">Resposta gerada, restrita ao conteúdo dos chunks recuperados.</param>
/// <param name="ChunksUsadosIds">
/// Identificadores dos chunks de conteúdo usados como contexto — permite rastrear de onde veio a
/// resposta e conferir que ela não é conhecimento solto do modelo.
/// </param>
public sealed record RespostaDuvida(string Resposta, IReadOnlyList<Guid> ChunksUsadosIds);
