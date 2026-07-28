using Forja.Domain.Questoes;

namespace Forja.Api;

/// <summary>
/// Representação de uma questão exposta ao usuário final. Não inclui <c>Gabarito</c> nem
/// <c>Explicacao</c> — essas informações só são reveladas na resposta de <c>POST /respostas</c>,
/// depois que o usuário já respondeu.
/// </summary>
/// <param name="Id">Identificador da questão.</param>
/// <param name="CarreiraId">Identificador da carreira à qual a questão se refere.</param>
/// <param name="BancaId">Identificador da banca relacionada, quando aplicável.</param>
/// <param name="DisciplinaId">Identificador da disciplina à qual a questão pertence.</param>
/// <param name="TopicoId">Identificador do tópico relacionado, quando aplicável.</param>
/// <param name="Tipo">Formato da questão.</param>
/// <param name="Enunciado">Enunciado da questão.</param>
/// <param name="Alternativas">Alternativas da questão, quando aplicável.</param>
public sealed record QuestaoResponse(
    Guid Id,
    Guid CarreiraId,
    Guid? BancaId,
    Guid DisciplinaId,
    Guid? TopicoId,
    string Tipo,
    string Enunciado,
    IReadOnlyList<Alternativa>? Alternativas)
{
    /// <summary>Constrói a resposta a partir da entidade de domínio.</summary>
    public static QuestaoResponse De(Questao questao) => new(
        questao.Id,
        questao.CarreiraId,
        questao.BancaId,
        questao.DisciplinaId,
        questao.TopicoId,
        questao.Tipo.ToString(),
        questao.Enunciado,
        questao.Alternativas);
}
