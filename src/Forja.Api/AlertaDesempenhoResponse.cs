using Forja.Application.Desempenho;

namespace Forja.Api;

/// <summary>Um alerta de queda de desempenho (<c>GET /desempenho/alertas</c>).</summary>
/// <param name="DisciplinaId">Identificador da disciplina.</param>
/// <param name="TaxaAcertoAnterior">Taxa de acerto (%) na janela anterior de respostas.</param>
/// <param name="TaxaAcertoRecente">Taxa de acerto (%) na janela mais recente de respostas.</param>
/// <param name="Mensagem">Mensagem de alerta pronta para exibir ao usuário.</param>
public sealed record AlertaDesempenhoResponse(Guid DisciplinaId, decimal TaxaAcertoAnterior, decimal TaxaAcertoRecente, string Mensagem)
{
    /// <summary>Constrói o item a partir do resultado do serviço.</summary>
    public static AlertaDesempenhoResponse De(AlertaDesempenho alerta) => new(
        alerta.DisciplinaId,
        alerta.TaxaAcertoAnterior,
        alerta.TaxaAcertoRecente,
        alerta.Mensagem);
}
