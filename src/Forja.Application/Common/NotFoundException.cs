namespace Forja.Application.Common;

/// <summary>
/// Lançada quando uma entidade solicitada por identificador não é encontrada (ou não está em um
/// estado visível ao chamador). Genérica para todos os casos de uso da Application — não crie uma
/// exceção específica por entidade para esse cenário.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>Nome da entidade não encontrada, para mensagens e mapeamento de resposta.</summary>
    public string Entidade { get; }

    /// <summary>Identificador que foi buscado.</summary>
    public object Id { get; }

    /// <summary>
    /// Cria uma nova instância da exceção.
    /// </summary>
    /// <param name="entidade">Nome da entidade não encontrada (ex.: "Questão").</param>
    /// <param name="id">Identificador que foi buscado.</param>
    public NotFoundException(string entidade, object id) : base($"{entidade} '{id}' não encontrado(a).")
    {
        Entidade = entidade;
        Id = id;
    }
}
