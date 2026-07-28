namespace Forja.Domain.Estudo;

/// <summary>
/// Representa um item (tópico agendado) dentro de um plano de estudo. Corresponde à tabela <c>plano_itens</c>.
/// </summary>
public class PlanoItem
{
    /// <summary>Identificador único do item do plano.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador do plano de estudo ao qual o item pertence.</summary>
    public Guid PlanoId { get; set; }

    /// <summary>Identificador do tópico agendado.</summary>
    public Guid TopicoId { get; set; }

    /// <summary>Posição de ordenação do item dentro do plano.</summary>
    public int Ordem { get; set; }

    /// <summary>Tempo alocado para o item, em minutos.</summary>
    public int TempoAlocadoMin { get; set; }

    /// <summary>Data prevista para a realização do item, quando definida.</summary>
    public DateOnly? DataPrevista { get; set; }

    /// <summary>Status de conclusão do item.</summary>
    public StatusItemPlano Status { get; set; } = StatusItemPlano.Pendente;
}
