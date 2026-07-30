using Forja.Application.Home;

namespace Forja.Api;

/// <summary>Resposta de <c>GET /home</c>.</summary>
/// <param name="DiasConsecutivos">Sequência atual de dias consecutivos de estudo.</param>
/// <param name="PontosTotal">Pontuação total acumulada.</param>
/// <param name="PontosSemanaAtual">Pontuação acumulada na semana de referência atual.</param>
/// <param name="PercentualPlanoConcluido">Percentual do plano de estudo atual já concluído, ou <c>null</c> se não há plano.</param>
/// <param name="RespostasHoje">Quantidade de respostas registradas hoje.</param>
/// <param name="AtividadesRecentes">Últimas atividades do usuário.</param>
public sealed record HomeResponse(
    int DiasConsecutivos,
    int PontosTotal,
    int PontosSemanaAtual,
    decimal? PercentualPlanoConcluido,
    int RespostasHoje,
    IReadOnlyList<AtividadeRecenteResponse> AtividadesRecentes)
{
    /// <summary>Constrói a resposta a partir do resultado do serviço.</summary>
    public static HomeResponse De(ResumoHome resumo) => new(
        resumo.DiasConsecutivos,
        resumo.PontosTotal,
        resumo.PontosSemanaAtual,
        resumo.PercentualPlanoConcluido,
        resumo.RespostasHoje,
        resumo.AtividadesRecentes.Select(AtividadeRecenteResponse.De).ToList());
}

/// <summary>Uma atividade recente do usuário.</summary>
/// <param name="QuestaoId">Identificador da questão respondida.</param>
/// <param name="Correta">Indica se a resposta estava correta.</param>
/// <param name="CriadoEm">Data e hora em que a resposta foi registrada.</param>
public sealed record AtividadeRecenteResponse(Guid QuestaoId, bool Correta, DateTimeOffset CriadoEm)
{
    /// <summary>Constrói o item a partir do resultado do serviço.</summary>
    public static AtividadeRecenteResponse De(AtividadeRecente atividade) => new(
        atividade.QuestaoId, atividade.Correta, atividade.CriadoEm);
}
