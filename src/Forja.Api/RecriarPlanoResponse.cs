using Forja.Application.Estudo;

namespace Forja.Api;

/// <summary>Resposta de <c>POST /plano/recriar</c>.</summary>
/// <param name="PlanoAnterior">
/// Resumo do plano que estava ativo antes desta recriação, ou <c>null</c> se não existia nenhum.
/// </param>
/// <param name="PlanoNovo">O plano recém-gerado, que passa a ser o atual.</param>
public sealed record RecriarPlanoResponse(ResumoPlanoAnteriorResponse? PlanoAnterior, PlanoAtualResponse PlanoNovo)
{
    /// <summary>Constrói a resposta a partir do resultado do serviço.</summary>
    public static RecriarPlanoResponse De(RecriarPlanoResultado resultado) => new(
        resultado.ResumoAnterior is null ? null : ResumoPlanoAnteriorResponse.De(resultado.ResumoAnterior),
        PlanoAtualResponse.De(resultado.PlanoNovo));
}
