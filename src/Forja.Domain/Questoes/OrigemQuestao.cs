namespace Forja.Domain.Questoes;

/// <summary>
/// Origem de criação da questão.
/// </summary>
public enum OrigemQuestao
{
    /// <summary>Corresponde a 'gerada_ia' no banco de dados.</summary>
    GeradaIA,

    /// <summary>Corresponde a 'reproduzida_prova_oficial' no banco de dados.</summary>
    ReproduzidaProvaOficial,

    /// <summary>Corresponde a 'inedita' no banco de dados.</summary>
    Inedita
}
