namespace Forja.Domain.Estudo;

/// <summary>
/// Status de conclusão de um item do plano de estudo.
/// </summary>
public enum StatusItemPlano
{
    /// <summary>Corresponde a 'pendente' no banco de dados.</summary>
    Pendente,

    /// <summary>Corresponde a 'concluido' no banco de dados.</summary>
    Concluido
}
