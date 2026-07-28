using Forja.Application.Common;
using Forja.Domain.Common;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="IRespostaService"/>.
/// </summary>
public class RespostaService : IRespostaService
{
    /// <summary>Pontos concedidos por uma resposta correta que pontua (RN-004).</summary>
    private const int PontosPorAcerto = 10;

    /// <summary>
    /// Tempo mínimo, em milissegundos, para uma resposta correta não ser tratada como chute (RN-009).
    /// </summary>
    private const int TempoMinimoMs = 5_000;

    private readonly IQuestaoRepository _questaoRepository;
    private readonly IRespostaUsuarioRepository _respostaRepository;
    private readonly IPontuacaoRepository _pontuacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    /// <param name="questaoRepository">Repositório de questões, para obter o gabarito.</param>
    /// <param name="respostaRepository">Repositório de respostas de usuário.</param>
    /// <param name="pontuacaoRepository">Repositório de pontuação de usuário.</param>
    /// <param name="unitOfWork">Unit of work para persistir resposta e pontuação em uma única transação.</param>
    public RespostaService(
        IQuestaoRepository questaoRepository,
        IRespostaUsuarioRepository respostaRepository,
        IPontuacaoRepository pontuacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _questaoRepository = questaoRepository;
        _respostaRepository = respostaRepository;
        _pontuacaoRepository = pontuacaoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<RegistrarRespostaResultado> RegistrarRespostaAsync(
        Guid usuarioId,
        Guid questaoId,
        string respostaDada,
        int tempoRespostaMs,
        Guid? pomodoroId,
        bool ehRevisao,
        CancellationToken cancellationToken = default)
    {
        var questao = await _questaoRepository.GetByIdAsync(questaoId, cancellationToken);
        if (questao is null || questao.Status != StatusQuestao.Aprovada)
        {
            throw new NotFoundException("Questão", questaoId);
        }

        var correta = string.Equals(respostaDada.Trim(), questao.Gabarito.Trim(), StringComparison.OrdinalIgnoreCase);
        var chute = tempoRespostaMs < TempoMinimoMs;
        var jaPontuada = await _respostaRepository.ExisteRespostaPontuadaAsync(usuarioId, questaoId, cancellationToken);

        // RN-008/RN-009: só pontua na primeira resposta correta, e nunca quando for chute.
        var pontua = correta && !chute && !jaPontuada;
        var pontosConcedidos = pontua ? PontosPorAcerto : 0;

        var resposta = new RespostaUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            QuestaoId = questaoId,
            PomodoroId = pomodoroId,
            RespostaDada = respostaDada,
            Correta = correta,
            TempoRespostaMs = tempoRespostaMs,
            Pontuada = pontua,
            PontosConcedidos = pontosConcedidos,
            EhRevisao = ehRevisao,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await _respostaRepository.AddAsync(resposta, cancellationToken);

        var pontuacao = await _pontuacaoRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (pontua)
        {
            var inicioSemanaAtual = InicioDaSemana(DateTimeOffset.UtcNow);

            if (pontuacao is null)
            {
                pontuacao = new Pontuacao
                {
                    UsuarioId = usuarioId,
                    PontosTotal = pontosConcedidos,
                    PontosSemanaAtual = pontosConcedidos,
                    SemanaReferencia = inicioSemanaAtual,
                };
                await _pontuacaoRepository.AddAsync(pontuacao, cancellationToken);
            }
            else
            {
                pontuacao.PontosTotal += pontosConcedidos;
                pontuacao.PontosSemanaAtual = pontuacao.SemanaReferencia == inicioSemanaAtual
                    ? pontuacao.PontosSemanaAtual + pontosConcedidos
                    : pontosConcedidos;
                pontuacao.SemanaReferencia = inicioSemanaAtual;
                _pontuacaoRepository.Update(pontuacao);
            }
        }

        // RN-010/RN-011/RN-012: resposta e pontuação persistem numa única chamada de SaveChanges —
        // se qualquer passo acima falhar antes daqui, nada é gravado.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegistrarRespostaResultado(resposta, questao, pontuacao ?? new Pontuacao { UsuarioId = usuarioId });
    }

    private static DateOnly InicioDaSemana(DateTimeOffset momento)
    {
        var data = DateOnly.FromDateTime(momento.UtcDateTime);
        var diasDesdeSegunda = ((int)data.DayOfWeek + 6) % 7;
        return data.AddDays(-diasDesdeSegunda);
    }
}
