namespace Forja.Domain.Catalogo;

/// <summary>
/// Peso de uma disciplina dentro de um edital, usado para priorizar a geração do plano de estudo
/// (RF-003). Calculado uma única vez por edital (não por plano gerado). Corresponde à tabela
/// <c>edital_peso_disciplina</c>, com chave composta <see cref="EditalId"/> + <see cref="DisciplinaId"/>.
/// </summary>
public class EditalPesoDisciplina
{
    /// <summary>Identificador do edital ao qual o peso se refere.</summary>
    public Guid EditalId { get; set; }

    /// <summary>Identificador da disciplina.</summary>
    public Guid DisciplinaId { get; set; }

    /// <summary>Peso relativo da disciplina dentro do edital.</summary>
    public decimal Peso { get; set; }

    /// <summary>Origem do cálculo do peso.</summary>
    public FontePesoDisciplina Fonte { get; set; }

    /// <summary>
    /// Identificador do edital de origem, quando <see cref="Fonte"/> é
    /// <see cref="FontePesoDisciplina.HerdadoEditalAnterior"/>.
    /// </summary>
    public Guid? EditalOrigemId { get; set; }

    /// <summary>Data e hora em que o peso foi calculado.</summary>
    public DateTimeOffset CalculadoEm { get; set; }
}
