using Forja.Domain.Estudo;

namespace Forja.Api;

/// <summary>Um item da fila de revisão pendente (<c>GET /revisao/pendente</c>).</summary>
/// <param name="QuestaoId">Identificador da questão a revisar.</param>
/// <param name="ErrosConsecutivos">Quantidade de erros consecutivos do usuário nessa questão.</param>
/// <param name="ProximaRevisaoEm">Data prevista para a revisão (já vencida, por isso está na fila).</param>
public sealed record RevisaoPendenteItem(Guid QuestaoId, int ErrosConsecutivos, DateOnly ProximaRevisaoEm)
{
    /// <summary>Constrói o item a partir da entidade de domínio.</summary>
    public static RevisaoPendenteItem De(RevisaoEspacada revisao) => new(
        revisao.QuestaoId,
        revisao.ErrosConsecutivos,
        revisao.ProximaRevisaoEm);
}
