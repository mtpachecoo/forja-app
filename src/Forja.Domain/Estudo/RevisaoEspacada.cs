namespace Forja.Domain.Estudo;

/// <summary>
/// Representa o estado de revisão espaçada de uma questão para um usuário.
/// Corresponde à tabela <c>revisao_espacada</c>.
/// </summary>
public class RevisaoEspacada
{
    /// <summary>Identificador único do registro de revisão espaçada.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Identificador da questão em revisão.</summary>
    public Guid QuestaoId { get; set; }

    /// <summary>Quantidade de erros consecutivos do usuário nessa questão.</summary>
    public int ErrosConsecutivos { get; set; }

    /// <summary>Intervalo atual, em dias, até a próxima revisão.</summary>
    public int IntervaloDiasAtual { get; set; } = 1;

    /// <summary>Data prevista para a próxima revisão.</summary>
    public DateOnly ProximaRevisaoEm { get; set; }
}
