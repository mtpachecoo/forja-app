namespace Forja.Domain.Conteudo;

/// <summary>
/// Tipo de fonte de conteúdo usada para gerar chunks e questões.
/// </summary>
public enum TipoFonte
{
    /// <summary>Corresponde a 'lei' no banco de dados.</summary>
    Lei,

    /// <summary>Corresponde a 'edital' no banco de dados.</summary>
    Edital,

    /// <summary>Corresponde a 'prova' no banco de dados.</summary>
    Prova
}
