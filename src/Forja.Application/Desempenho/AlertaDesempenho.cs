namespace Forja.Application.Desempenho;

/// <summary>
/// Alerta de queda de desempenho detectada numa disciplina (RF-021).
/// </summary>
/// <param name="DisciplinaId">Identificador da disciplina.</param>
/// <param name="TaxaAcertoAnterior">Taxa de acerto (%) na janela anterior de respostas.</param>
/// <param name="TaxaAcertoRecente">Taxa de acerto (%) na janela mais recente de respostas.</param>
/// <param name="Mensagem">Mensagem de alerta pronta para exibir ao usuário.</param>
public sealed record AlertaDesempenho(Guid DisciplinaId, decimal TaxaAcertoAnterior, decimal TaxaAcertoRecente, string Mensagem);
