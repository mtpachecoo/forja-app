namespace Forja.Domain.Catalogo;

/// <summary>
/// Representa uma disciplina de estudo. Corresponde à tabela <c>disciplinas</c>.
/// </summary>
public class Disciplina
{
    /// <summary>Identificador único da disciplina.</summary>
    public Guid Id { get; set; }

    /// <summary>Nome da disciplina. Único no sistema.</summary>
    public string Nome { get; set; } = string.Empty;
}
