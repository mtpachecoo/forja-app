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
}
