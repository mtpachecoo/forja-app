namespace Forja.Domain.Questoes;

/// <summary>
/// Formato da questão.
/// </summary>
public enum TipoQuestao
{
    /// <summary>Corresponde a 'certo_errado' no banco de dados.</summary>
    CertoErrado,

    /// <summary>Corresponde a 'multipla_escolha' no banco de dados.</summary>
    MultiplaEscolha
}
