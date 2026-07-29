using Forja.Application.Common;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IPomodoroService"/>.
/// </summary>
public class PomodoroService : IPomodoroService
{
    /// <summary>Pontos concedidos pela conclusão de um pomodoro com pelo menos uma resposta registrada.</summary>
    private const int PontosPorPomodoroConcluido = 5;

    private readonly ISessaoEstudoRepository _sessaoEstudoRepository;
    private readonly IPomodoroRepository _pomodoroRepository;
    private readonly IRespostaUsuarioRepository _respostaRepository;
    private readonly IPontuacaoRepository _pontuacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public PomodoroService(
        ISessaoEstudoRepository sessaoEstudoRepository,
        IPomodoroRepository pomodoroRepository,
        IRespostaUsuarioRepository respostaRepository,
        IPontuacaoRepository pontuacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _sessaoEstudoRepository = sessaoEstudoRepository;
        _pomodoroRepository = pomodoroRepository;
        _respostaRepository = respostaRepository;
        _pontuacaoRepository = pontuacaoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Pomodoro> IniciarPomodoroAsync(Guid usuarioId, Guid sessaoId, CancellationToken cancellationToken = default)
    {
        await ObterSessaoDoUsuarioAsync(usuarioId, sessaoId, cancellationToken);

        var pomodoro = new Pomodoro
        {
            Id = Guid.NewGuid(),
            SessaoId = sessaoId,
            IniciadoEm = DateTimeOffset.UtcNow,
        };

        await _pomodoroRepository.AddAsync(pomodoro, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return pomodoro;
    }

    /// <inheritdoc />
    public async Task<FinalizarPomodoroResultado> FinalizarPomodoroAsync(
        Guid usuarioId,
        Guid sessaoId,
        Guid pomodoroId,
        CancellationToken cancellationToken = default)
    {
        await ObterSessaoDoUsuarioAsync(usuarioId, sessaoId, cancellationToken);

        var pomodoro = await _pomodoroRepository.GetByIdAsync(pomodoroId, cancellationToken);
        if (pomodoro is null || pomodoro.SessaoId != sessaoId)
        {
            throw new NotFoundException("Pomodoro", pomodoroId);
        }

        if (pomodoro.FinalizadoEm is not null)
        {
            // Já finalizado — idempotente, não concede pontos de novo.
            return new FinalizarPomodoroResultado(pomodoro, null);
        }

        var qtdRespostas = await _respostaRepository.ContarRespostasNoPomodoroAsync(pomodoroId, cancellationToken);

        pomodoro.FinalizadoEm = DateTimeOffset.UtcNow;
        pomodoro.QtdRespostasNoCiclo = qtdRespostas;

        // RN-011: só concede pontos se houve pelo menos uma resposta registrada no ciclo — o timer ter
        // rodado até o fim, por si só, não pontua.
        Pontuacao? pontuacaoAtualizada = null;
        if (qtdRespostas > 0)
        {
            pomodoro.PontosConcedidos = PontosPorPomodoroConcluido;

            var pontuacaoExistente = await _pontuacaoRepository.GetByIdAsync(usuarioId, cancellationToken);
            var pontuacaoJaExistia = pontuacaoExistente is not null;
            pontuacaoAtualizada = pontuacaoExistente ?? new Pontuacao { UsuarioId = usuarioId };
            pontuacaoAtualizada.RegistrarPontos(PontosPorPomodoroConcluido, DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime));

            if (pontuacaoJaExistia)
            {
                _pontuacaoRepository.Update(pontuacaoAtualizada);
            }
            else
            {
                await _pontuacaoRepository.AddAsync(pontuacaoAtualizada, cancellationToken);
            }
        }

        _pomodoroRepository.Update(pomodoro);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new FinalizarPomodoroResultado(pomodoro, pontuacaoAtualizada);
    }

    private async Task<SessaoEstudo> ObterSessaoDoUsuarioAsync(Guid usuarioId, Guid sessaoId, CancellationToken cancellationToken)
    {
        var sessao = await _sessaoEstudoRepository.GetByIdAsync(sessaoId, cancellationToken);
        if (sessao is null || sessao.UsuarioId != usuarioId)
        {
            throw new NotFoundException("Sessão de estudo", sessaoId);
        }

        return sessao;
    }
}
