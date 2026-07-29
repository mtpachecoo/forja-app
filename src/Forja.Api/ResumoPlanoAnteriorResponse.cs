using Forja.Application.Estudo;

namespace Forja.Api;

/// <summary>Resumo do plano anterior, devolvido por <c>POST /plano/recriar</c> antes de ser substituído.</summary>
/// <param name="TotalTopicos">Quantidade total de tópicos que o plano anterior tinha.</param>
/// <param name="TopicosConcluidos">Quantidade de tópicos já concluídos no plano anterior.</param>
/// <param name="DiasRestantesAteProva">Dias restantes até a prova, ou <c>null</c> se a data ainda não foi divulgada.</param>
public sealed record ResumoPlanoAnteriorResponse(int TotalTopicos, int TopicosConcluidos, int? DiasRestantesAteProva)
{
    /// <summary>Constrói a resposta a partir do resultado do serviço.</summary>
    public static ResumoPlanoAnteriorResponse De(ResumoPlanoAnterior resumo) => new(
        resumo.TotalTopicos,
        resumo.TopicosConcluidos,
        resumo.DiasRestantesAteProva);
}
