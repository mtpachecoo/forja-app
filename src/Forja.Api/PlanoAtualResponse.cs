using Forja.Application.Estudo;
using Forja.Domain.Estudo;

namespace Forja.Api;

/// <summary>Resposta de <c>GET /plano/atual</c>.</summary>
/// <param name="PlanoId">Identificador do plano de estudo.</param>
/// <param name="CarreiraId">Identificador da carreira.</param>
/// <param name="GeradoViaIA">Indica se o plano foi gerado por inteligência artificial.</param>
/// <param name="Itens">Itens do plano, na ordem de prioridade sugerida.</param>
public sealed record PlanoAtualResponse(Guid PlanoId, Guid CarreiraId, bool GeradoViaIA, IReadOnlyList<PlanoItemResponse> Itens)
{
    /// <summary>Constrói a resposta a partir do resultado do serviço.</summary>
    public static PlanoAtualResponse De(PlanoGerado plano) => new(
        plano.Plano.Id,
        plano.Plano.CarreiraId,
        plano.Plano.GeradoViaIA,
        plano.Itens.Select(PlanoItemResponse.De).ToList());
}

/// <summary>Um item do plano de estudo.</summary>
/// <param name="TopicoId">Identificador do tópico.</param>
/// <param name="Ordem">Posição de ordenação/prioridade do item.</param>
/// <param name="TempoAlocadoMin">Tempo alocado para o item, em minutos.</param>
/// <param name="DataPrevista">Data prevista para a realização do item, quando definida.</param>
/// <param name="Status">Status de conclusão do item.</param>
public sealed record PlanoItemResponse(Guid TopicoId, int Ordem, int TempoAlocadoMin, DateOnly? DataPrevista, string Status)
{
    /// <summary>Constrói o item a partir da entidade de domínio.</summary>
    public static PlanoItemResponse De(PlanoItem item) => new(
        item.TopicoId,
        item.Ordem,
        item.TempoAlocadoMin,
        item.DataPrevista,
        item.Status.ToString());
}
