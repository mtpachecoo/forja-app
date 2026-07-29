using Forja.Domain.Usuarios;

namespace Forja.Application.Estudo;

/// <summary>
/// Serviço de geração do plano de estudo inicial (RF-003).
/// </summary>
public interface IPlanoEstudoService
{
    /// <summary>
    /// Obtém o plano de estudo atual do usuário para a carreira informada, gerando-o pela primeira vez
    /// se ainda não existir. O plano é priorizado pelo peso de disciplina do edital da carreira
    /// (ver <see cref="IPesoDisciplinaService"/>) e, quando disponível, pela proximidade da data da
    /// prova. Nunca inclui um tópico fora do catálogo real do edital.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="carreiraId">Identificador da carreira.</param>
    /// <param name="tempoDisponivelMinDia">Tempo disponível do usuário por dia de estudo, em minutos.</param>
    /// <param name="nivel">Nível de conhecimento declarado pelo usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O plano de estudo (existente ou recém-gerado) e seus itens.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">
    /// Lançada quando a carreira não tem nenhum edital, ou o edital não tem nenhum tópico cadastrado.
    /// </exception>
    Task<PlanoGerado> ObterOuGerarPlanoAtualAsync(
        Guid usuarioId,
        Guid carreiraId,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gera um novo plano de estudo, mesmo que já exista um ativo — útil quando um edital novo
    /// (ou tópicos novos) entrou no catálogo. O plano anterior <b>não é apagado</b>: continua no banco
    /// como histórico, e só deixa de ser "o ativo" porque <see cref="ObterOuGerarPlanoAtualAsync"/>
    /// sempre retorna o mais recente por data de criação.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="carreiraId">Identificador da carreira.</param>
    /// <param name="tempoDisponivelMinDia">Tempo disponível do usuário por dia de estudo, em minutos.</param>
    /// <param name="nivel">Nível de conhecimento declarado pelo usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>
    /// Um resumo do plano anterior (se existia um) e o plano novo, que passa a ser o atual.
    /// </returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">
    /// Lançada quando a carreira não tem nenhum edital, ou o edital não tem nenhum tópico cadastrado.
    /// </exception>
    Task<RecriarPlanoResultado> RecriarPlanoAsync(
        Guid usuarioId,
        Guid carreiraId,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken = default);
}
