namespace Forja.Domain.Contribuicao;

/// <summary>
/// Status de moderação de uma contribuição de conteúdo. Mesmo padrão de nomeação de
/// <see cref="Forja.Domain.Questoes.StatusQuestao"/> (em_revisao/aprovada/rejeitada), enum próprio —
/// contribuição e questão são domínios diferentes, sem estado "rascunho".
/// </summary>
public enum StatusContribuicao
{
    /// <summary>Corresponde a 'em_revisao' no banco de dados. Estado inicial de toda contribuição submetida.</summary>
    EmRevisao,

    /// <summary>Corresponde a 'aprovada' no banco de dados.</summary>
    Aprovada,

    /// <summary>Corresponde a 'rejeitada' no banco de dados.</summary>
    Rejeitada
}
