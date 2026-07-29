namespace Forja.Domain.Gamificacao;

/// <summary>
/// Representa a pontuação acumulada de um usuário. Corresponde à tabela <c>pontuacoes</c>.
/// </summary>
public class Pontuacao
{
    /// <summary>Identificador do usuário. Chave primária da tabela.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Pontuação total acumulada pelo usuário.</summary>
    public int PontosTotal { get; set; }

    /// <summary>Pontuação acumulada na semana de referência atual.</summary>
    public int PontosSemanaAtual { get; set; }

    /// <summary>Data de início da semana de referência para <see cref="PontosSemanaAtual"/>.</summary>
    public DateOnly SemanaReferencia { get; set; }

    /// <summary>
    /// Registra pontos ganhos em <paramref name="hoje"/>, acumulando em <see cref="PontosTotal"/> e em
    /// <see cref="PontosSemanaAtual"/> — a menos que <paramref name="hoje"/> caia numa semana diferente
    /// de <see cref="SemanaReferencia"/>, caso em que <see cref="PontosSemanaAtual"/> reinicia contando
    /// só os pontos desta chamada.
    /// </summary>
    /// <param name="pontos">Pontos a adicionar.</param>
    /// <param name="hoje">Data em que os pontos foram ganhos.</param>
    public void RegistrarPontos(int pontos, DateOnly hoje)
    {
        var inicioSemana = InicioDaSemana(hoje);

        PontosSemanaAtual = SemanaReferencia == inicioSemana
            ? PontosSemanaAtual + pontos
            : pontos;

        PontosTotal += pontos;
        SemanaReferencia = inicioSemana;
    }

    /// <summary>
    /// Data de início (segunda-feira) da semana de referência que contém <paramref name="data"/>.
    /// Compartilhado com quem precisa localizar a mesma "semana atual" usada por
    /// <see cref="SemanaReferencia"/> — ex.: o ranking semanal (RF-013).
    /// </summary>
    public static DateOnly InicioDaSemana(DateOnly data)
    {
        var diasDesdeSegunda = ((int)data.DayOfWeek + 6) % 7;
        return data.AddDays(-diasDesdeSegunda);
    }
}
