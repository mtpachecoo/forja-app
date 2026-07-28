namespace Forja.Domain.Gamificacao;

/// <summary>
/// Representa uma medalha que pode ser conquistada pelos usuários. Corresponde à tabela <c>medalhas</c>.
/// </summary>
public class Medalha
{
    /// <summary>Identificador único da medalha.</summary>
    public Guid Id { get; set; }

    /// <summary>Nome da medalha.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Descrição do critério necessário para conquistar a medalha.</summary>
    public string Criterio { get; set; } = string.Empty;
}
