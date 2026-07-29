namespace Forja.Application.Estudo;

/// <summary>
/// Resultado de <c>POST /plano/recriar</c>.
/// </summary>
/// <param name="ResumoAnterior">
/// Resumo do plano que estava ativo antes desta recriação, ou <c>null</c> se o usuário ainda não
/// tinha nenhum plano para essa carreira.
/// </param>
/// <param name="PlanoNovo">O plano recém-gerado, que passa a ser o atual.</param>
public sealed record RecriarPlanoResultado(ResumoPlanoAnterior? ResumoAnterior, PlanoGerado PlanoNovo);
