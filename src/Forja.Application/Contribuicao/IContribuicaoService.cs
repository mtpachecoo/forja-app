using Forja.Domain.Contribuicao;

namespace Forja.Application.Contribuicao;

/// <summary>
/// Serviço de aplicação de submissão e moderação de contribuições de conteúdo externo (vídeo/PDF) por
/// tópico, e da reputação por contribuição que resulta da aprovação (distinta de <see cref="Forja.Domain.Gamificacao.Pontuacao"/>,
/// que mede estudo).
/// </summary>
public interface IContribuicaoService
{
    /// <summary>
    /// Submete uma nova contribuição, sempre com status inicial <see cref="StatusContribuicao.EmRevisao"/>.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário que está contribuindo.</param>
    /// <param name="topicoId">Identificador do tópico ao qual a contribuição se refere.</param>
    /// <param name="tipo">Tipo do conteúdo submetido.</param>
    /// <param name="link">Link (URL absoluta) do conteúdo externo.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A contribuição criada.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">Lançada quando o tópico não existe.</exception>
    /// <exception cref="ArgumentException">Lançada quando <paramref name="link"/> não é uma URL absoluta válida.</exception>
    Task<ContribuicaoConteudo> SubmeterAsync(
        Guid usuarioId, Guid topicoId, TipoContribuicao tipo, string link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista as contribuições aprovadas de um tópico, mais recentes primeiro, paginado.
    /// </summary>
    /// <param name="topicoId">Identificador do tópico.</param>
    /// <param name="skip">Quantidade de posições a pular (paginação).</param>
    /// <param name="take">Quantidade máxima de posições a retornar (paginação).</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista somente leitura das contribuições aprovadas do tópico.</returns>
    Task<IReadOnlyList<ContribuicaoConteudo>> ListarAprovadasPorTopicoAsync(
        Guid topicoId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aprova uma contribuição: marca o status, concede pontos de reputação ao autor e, na primeira
    /// aprovação do autor, a medalha de marco correspondente.
    /// </summary>
    /// <param name="contribuicaoId">Identificador da contribuição.</param>
    /// <param name="moderadorUsuarioId">Identificador do usuário que está moderando.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A contribuição aprovada.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">Lançada quando o moderador ou a contribuição não existem.</exception>
    /// <exception cref="Forja.Application.Common.NaoAutorizadoException">Lançada quando o moderador não é administrador.</exception>
    Task<ContribuicaoConteudo> AprovarAsync(Guid contribuicaoId, Guid moderadorUsuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejeita uma contribuição: só marca o status, sem efeito em reputação.
    /// </summary>
    /// <param name="contribuicaoId">Identificador da contribuição.</param>
    /// <param name="moderadorUsuarioId">Identificador do usuário que está moderando.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>A contribuição rejeitada.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">Lançada quando o moderador ou a contribuição não existem.</exception>
    /// <exception cref="Forja.Application.Common.NaoAutorizadoException">Lançada quando o moderador não é administrador.</exception>
    Task<ContribuicaoConteudo> RejeitarAsync(Guid contribuicaoId, Guid moderadorUsuarioId, CancellationToken cancellationToken = default);
}
