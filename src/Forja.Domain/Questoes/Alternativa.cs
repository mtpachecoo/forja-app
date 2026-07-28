namespace Forja.Domain.Questoes;

/// <summary>
/// Representa uma alternativa de questão de múltipla escolha.
/// </summary>
/// <param name="Letra">Identificador da alternativa (A, B, C, D ou E).</param>
/// <param name="Texto">Texto da alternativa.</param>
public sealed record Alternativa(string Letra, string Texto);
