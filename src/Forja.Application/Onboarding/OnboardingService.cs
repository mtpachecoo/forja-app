using Forja.Application.Common;
using Forja.Application.Estudo;
using Forja.Domain.Common;
using Forja.Domain.Usuarios;

namespace Forja.Application.Onboarding;

/// <summary>
/// Implementação padrão de <see cref="IOnboardingService"/>.
/// </summary>
public class OnboardingService : IOnboardingService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoEstudoService _planoEstudoService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public OnboardingService(
        IUsuarioRepository usuarioRepository,
        IPlanoEstudoService planoEstudoService,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _planoEstudoService = planoEstudoService;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<OnboardingResultado> CompletarOnboardingAsync(
        Guid usuarioId,
        Guid carreiraId,
        int tempoDisponivelMinDia,
        NivelUsuario nivel,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken)
            ?? throw new NotFoundException("Usuário", usuarioId);

        usuario.TempoDisponivelMinDia = tempoDisponivelMinDia;
        usuario.Nivel = nivel;
        _usuarioRepository.Update(usuario);

        var plano = await _planoEstudoService.ObterOuGerarPlanoAtualAsync(
            usuarioId, carreiraId, tempoDisponivelMinDia, nivel, cancellationToken);

        // ObterOuGerarPlanoAtualAsync só persiste quando gera um plano novo — se o usuário já tinha um
        // plano pra essa carreira (re-onboarding), ele retorna sem salvar nada, e a atualização de
        // Usuario acima ficaria só rastreada em memória. SaveChangesAsync aqui é sempre necessário;
        // chamá-lo de novo quando o plano já foi salvo internamente é inofensivo (nada pendente).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OnboardingResultado(usuario, plano);
    }
}
