using Forja.Application.Estudo;
using Forja.Domain.Usuarios;

namespace Forja.Application.Onboarding;

/// <summary>
/// Serviço de aplicação do onboarding inicial (RF-002): recebe o contexto que só o usuário sabe
/// informar (carreira, tempo disponível, nível) e gera o primeiro plano de estudo já com esse contexto.
/// </summary>
public interface IOnboardingService
{
    /// <summary>
    /// Atualiza o perfil do usuário com os dados de onboarding e gera (ou reaproveita) o plano de
    /// estudo atual para a carreira informada, via <see cref="IPlanoEstudoService.ObterOuGerarPlanoAtualAsync"/>.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário autenticado.</param>
    /// <param name="carreiraId">Carreira escolhida para o plano de estudo.</param>
    /// <param name="tempoDisponivelMinDia">Tempo disponível por dia de estudo, em minutos.</param>
    /// <param name="nivel">Nível de conhecimento declarado pelo usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O usuário atualizado e o plano de estudo resultante.</returns>
    /// <exception cref="Forja.Application.Common.NotFoundException">
    /// Lançada quando o usuário não existe, quando a carreira não tem edital, ou o edital não tem
    /// nenhum tópico cadastrado.
    /// </exception>
    Task<OnboardingResultado> CompletarOnboardingAsync(
        Guid usuarioId,
        Guid carreiraId,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken = default);
}

/// <summary>Resultado do onboarding: usuário com o contexto já preenchido e o plano gerado a partir dele.</summary>
/// <param name="Usuario">Usuário com <see cref="Usuario.TempoDisponivelMinDia"/> e <see cref="Usuario.Nivel"/> atualizados.</param>
/// <param name="Plano">Plano de estudo atual, gerado (ou reaproveitado) usando o contexto informado.</param>
public sealed record OnboardingResultado(Usuario Usuario, PlanoGerado Plano);
