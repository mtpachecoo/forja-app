using Forja.Application.Gamificacao;
using Forja.Domain.Common;
using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IIniciarSessaoComEfeitosService"/>.
/// </summary>
public class IniciarSessaoComEfeitosService : IIniciarSessaoComEfeitosService
{
    private readonly ISessaoEstudoService _sessaoEstudoService;
    private readonly IStreakService _streakService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public IniciarSessaoComEfeitosService(ISessaoEstudoService sessaoEstudoService, IStreakService streakService, IUnitOfWork unitOfWork)
    {
        _sessaoEstudoService = sessaoEstudoService;
        _streakService = streakService;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<SessaoEstudo> IniciarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var sessao = await _sessaoEstudoService.IniciarSessaoAsync(usuarioId, cancellationToken);
        await _streakService.RegistrarAtividadeAsync(usuarioId, cancellationToken);

        // Ponto único de commit: se o streak lançar, esta linha nunca é alcançada e a sessão nunca
        // fica persistida sozinha.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return sessao;
    }
}
