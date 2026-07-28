namespace Forja.Domain.Questoes;

/// <summary>
/// Status de revisão editorial da questão.
/// </summary>
public enum StatusQuestao
{
    /// <summary>Corresponde a 'rascunho' no banco de dados.</summary>
    Rascunho,

    /// <summary>Corresponde a 'em_revisao' no banco de dados.</summary>
    EmRevisao,

    /// <summary>Corresponde a 'aprovada' no banco de dados.</summary>
    Aprovada,

    /// <summary>Corresponde a 'rejeitada' no banco de dados.</summary>
    Rejeitada
}
