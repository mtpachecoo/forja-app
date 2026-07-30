using System.Collections.Frozen;

namespace Forja.Domain.Gamificacao;

/// <summary>
/// Calcula o <see cref="NivelReputacao"/> de um usuário a partir dos pontos de contribuição
/// acumulados. Função pura, sem dependência injetada — os limiares são dado estático (não mudam em
/// runtime), por isso <see cref="FrozenDictionary{TKey,TValue}"/> em vez de <see cref="Dictionary{TKey,TValue}"/>.
/// </summary>
public static class ReputacaoCalculator
{
    /// <summary>
    /// Limiares mínimos de pontos por nível — calibração inicial sem RN prévia pra herdar (reputação
    /// por contribuição é mecânica nova nesta fase); ajustar aqui conforme dado real de uso se os
    /// níveis se mostrarem rápidos/lentos demais de alcançar.
    /// </summary>
    private static readonly FrozenDictionary<NivelReputacao, int> LimiaresMinimos = new Dictionary<NivelReputacao, int>
    {
        [NivelReputacao.Bronze] = 0,
        [NivelReputacao.Prata] = 50,
        [NivelReputacao.Ouro] = 150,
        [NivelReputacao.Platina] = 400,
    }.ToFrozenDictionary();

    /// <summary>
    /// Calcula o nível de reputação correspondente aos pontos informados: o maior nível cujo limiar
    /// mínimo é menor ou igual a <paramref name="pontos"/>.
    /// </summary>
    /// <param name="pontos">Pontos de contribuição acumulados (negativo é tratado como 0).</param>
    /// <returns>O nível de reputação correspondente.</returns>
    public static NivelReputacao CalcularNivel(int pontos)
    {
        var pontosNaoNegativos = Math.Max(pontos, 0);

        var nivel = NivelReputacao.Bronze;
        foreach (var (candidato, limiar) in LimiaresMinimos.OrderBy(par => par.Value))
        {
            if (pontosNaoNegativos >= limiar)
            {
                nivel = candidato;
            }
        }

        return nivel;
    }
}
