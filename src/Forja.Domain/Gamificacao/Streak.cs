namespace Forja.Domain.Gamificacao;

/// <summary>
/// Representa a sequência de dias consecutivos de estudo de um usuário. Corresponde à tabela <c>streaks</c>.
/// </summary>
public class Streak
{
    /// <summary>Identificador do usuário. Chave primária da tabela.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Quantidade de dias consecutivos de estudo.</summary>
    public int DiasConsecutivos { get; set; }

    /// <summary>Data da última atividade registrada, quando houver.</summary>
    public DateOnly? UltimaAtividadeEm { get; set; }
}
