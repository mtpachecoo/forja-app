using Forja.Domain.Estudo;

namespace Forja.Api;

/// <summary>Resposta de <c>POST /sessao/iniciar</c>.</summary>
/// <param name="Id">Identificador da sessão de estudo.</param>
/// <param name="DataSessao">Data da sessão.</param>
public sealed record SessaoResponse(Guid Id, DateOnly DataSessao)
{
    /// <summary>Constrói a resposta a partir da entidade de domínio.</summary>
    public static SessaoResponse De(SessaoEstudo sessao) => new(sessao.Id, sessao.DataSessao);
}
