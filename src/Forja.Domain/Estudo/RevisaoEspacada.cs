namespace Forja.Domain.Estudo;

/// <summary>
/// Representa o estado de revisão espaçada de uma questão para um usuário.
/// Corresponde à tabela <c>revisao_espacada</c>.
/// </summary>
public class RevisaoEspacada
{
    /// <summary>Intervalo mínimo (dias) para o qual o intervalo reseta após um erro.</summary>
    private const int IntervaloMinimoDias = 1;

    /// <summary>Identificador único do registro de revisão espaçada.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do usuário.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Identificador da questão em revisão.</summary>
    public Guid QuestaoId { get; set; }

    /// <summary>Quantidade de erros consecutivos do usuário nessa questão.</summary>
    public int ErrosConsecutivos { get; set; }

    /// <summary>Intervalo atual, em dias, até a próxima revisão.</summary>
    public int IntervaloDiasAtual { get; set; } = IntervaloMinimoDias;

    /// <summary>Data prevista para a próxima revisão.</summary>
    public DateOnly ProximaRevisaoEm { get; set; }

    /// <summary>
    /// Registra o resultado de uma resposta e recalcula o agendamento da próxima revisão (RN-003):
    /// acerto zera <see cref="ErrosConsecutivos"/> e dobra <see cref="IntervaloDiasAtual"/> (revisa
    /// mais tarde); erro incrementa <see cref="ErrosConsecutivos"/> e reseta o intervalo para o
    /// mínimo (revisa de novo mais cedo) — não o contrário.
    /// </summary>
    /// <param name="correta">Indica se a resposta estava correta.</param>
    /// <param name="hoje">Data em que a resposta foi registrada.</param>
    public void RegistrarResultado(bool correta, DateOnly hoje)
    {
        if (correta)
        {
            ErrosConsecutivos = 0;
            IntervaloDiasAtual *= 2;
        }
        else
        {
            ErrosConsecutivos += 1;
            IntervaloDiasAtual = IntervaloMinimoDias;
        }

        ProximaRevisaoEm = hoje.AddDays(IntervaloDiasAtual);
    }
}
