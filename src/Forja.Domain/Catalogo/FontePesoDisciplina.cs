namespace Forja.Domain.Catalogo;

/// <summary>
/// Origem do peso calculado para uma disciplina dentro de um edital, em <see cref="EditalPesoDisciplina"/>.
/// </summary>
public enum FontePesoDisciplina
{
    /// <summary>Peso extraído via IA de um chunk do próprio edital com a distribuição de questões por disciplina.</summary>
    ExtraidoEdital,

    /// <summary>Peso calculado pela contagem de questões aprovadas por disciplina.</summary>
    ContagemQuestoes,

    /// <summary>Peso copiado do edital anterior mais recente da mesma carreira.</summary>
    HerdadoEditalAnterior,
}
