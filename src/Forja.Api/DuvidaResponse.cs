namespace Forja.Api;

/// <summary>
/// Resposta de <c>POST /duvidas</c>.
/// </summary>
/// <param name="Resposta">Resposta gerada, restrita ao conteúdo dos chunks recuperados.</param>
/// <param name="ChunksUsadosIds">Identificadores dos chunks de conteúdo usados como contexto.</param>
public sealed record DuvidaResponse(string Resposta, IReadOnlyList<Guid> ChunksUsadosIds);
