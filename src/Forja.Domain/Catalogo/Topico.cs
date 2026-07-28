namespace Forja.Domain.Catalogo;

/// <summary>
/// Representa um tópico de estudo dentro de uma disciplina e de um edital. Corresponde à tabela <c>topicos</c>.
/// </summary>
public class Topico
{
    /// <summary>Identificador único do tópico.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do edital ao qual o tópico pertence.</summary>
    public Guid EditalId { get; set; }

    /// <summary>Identificador da disciplina à qual o tópico pertence.</summary>
    public Guid DisciplinaId { get; set; }

    /// <summary>Nome do tópico.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Posição de ordenação do tópico dentro da disciplina/edital.</summary>
    public int Ordem { get; set; }
}
