namespace Forja.Application.Estudo;

/// <summary>
/// Peso relativo de uma disciplina, usado para priorizar a geração do plano de estudo (RF-003).
/// Contrato enxuto de retorno de <see cref="IPesoDisciplinaService"/> — não carrega metadados de
/// persistência (fonte, edital de origem), que são um detalhe interno do cálculo.
/// </summary>
/// <param name="DisciplinaId">Identificador da disciplina.</param>
/// <param name="Peso">Peso relativo da disciplina.</param>
public sealed record PesoDisciplina(Guid DisciplinaId, decimal Peso);
