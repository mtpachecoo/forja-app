using Forja.Domain.Common;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IRegistrarRespostaComEfeitosService"/>.
/// </summary>
public class RegistrarRespostaComEfeitosService : IRegistrarRespostaComEfeitosService
{
    private readonly IRespostaService _respostaService;
    private readonly IRevisaoEspacadaService _revisaoEspacadaService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public RegistrarRespostaComEfeitosService(
        IRespostaService respostaService,
        IRevisaoEspacadaService revisaoEspacadaService,
        IUnitOfWork unitOfWork)
    {
        _respostaService = respostaService;
        _revisaoEspacadaService = revisaoEspacadaService;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<RegistrarRespostaResultado> RegistrarAsync(
        Guid usuarioId,
        Guid questaoId,
        string respostaDada,
        int tempoRespostaMs,
        Guid? pomodoroId,
        bool ehRevisao,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _respostaService.RegistrarRespostaAsync(
            usuarioId,
            questaoId,
            respostaDada,
            tempoRespostaMs,
            pomodoroId,
            ehRevisao,
            cancellationToken);

        // RN-003: toda resposta atualiza o estado de revisão espaçada da questão (RF-010).
        await _revisaoEspacadaService.RegistrarRespostaAsync(usuarioId, questaoId, resultado.Resposta.Correta, cancellationToken);

        // Ponto único de commit: se qualquer chamada acima tiver lançado, esta linha nunca é
        // alcançada e nada fica persistido — resposta e revisão espaçada só existem juntas.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return resultado;
    }
}
