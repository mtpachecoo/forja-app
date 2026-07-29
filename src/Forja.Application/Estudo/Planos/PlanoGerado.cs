using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Resultado da obtenção/geração do plano de estudo atual de um usuário para uma carreira (RF-003).
/// </summary>
/// <param name="Plano">O plano de estudo.</param>
/// <param name="Itens">Os itens do plano, na ordem de prioridade sugerida.</param>
public sealed record PlanoGerado(PlanoEstudo Plano, IReadOnlyList<PlanoItem> Itens);
