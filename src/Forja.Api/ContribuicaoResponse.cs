using Forja.Domain.Contribuicao;

namespace Forja.Api;

/// <summary>Representa uma contribuição de conteúdo nas respostas da API.</summary>
/// <param name="Id">Identificador da contribuição.</param>
/// <param name="UsuarioId">Identificador do usuário que contribuiu.</param>
/// <param name="TopicoId">Identificador do tópico.</param>
/// <param name="Tipo">Tipo do conteúdo.</param>
/// <param name="Link">Link do conteúdo externo.</param>
/// <param name="Status">Status de moderação.</param>
/// <param name="CriadoEm">Data e hora de submissão.</param>
public sealed record ContribuicaoResponse(
    Guid Id, Guid UsuarioId, Guid TopicoId, string Tipo, string Link, string Status, DateTimeOffset CriadoEm)
{
    /// <summary>Constrói a resposta a partir da entidade de domínio.</summary>
    public static ContribuicaoResponse De(ContribuicaoConteudo contribuicao) => new(
        contribuicao.Id,
        contribuicao.UsuarioId,
        contribuicao.TopicoId,
        contribuicao.Tipo.ToString(),
        contribuicao.Link,
        contribuicao.Status.ToString(),
        contribuicao.CriadoEm);
}
