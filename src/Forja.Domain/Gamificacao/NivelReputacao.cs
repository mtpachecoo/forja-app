namespace Forja.Domain.Gamificacao;

/// <summary>
/// Nível de reputação por contribuição, calculado a partir de <see cref="ReputacaoContribuicao.PontosContribuicao"/>
/// via <see cref="ReputacaoCalculator"/>. Puramente derivado — não é uma coluna persistida.
/// </summary>
public enum NivelReputacao
{
    /// <summary>Nível inicial, a partir de 0 pontos.</summary>
    Bronze,

    /// <summary>Ver limiar em <see cref="ReputacaoCalculator"/>.</summary>
    Prata,

    /// <summary>Ver limiar em <see cref="ReputacaoCalculator"/>.</summary>
    Ouro,

    /// <summary>Nível máximo. Ver limiar em <see cref="ReputacaoCalculator"/>.</summary>
    Platina
}
