using Forja.Application.Questoes;
using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;

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

    private readonly IQuestaoService _questaoService;
    private readonly IRespostaUsuarioRepository _respostaRepository;
    private readonly IPontuacaoRepository _pontuacaoRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    /// <param name="questaoService">Serviço de questões, para obter a questão aprovada e seu gabarito.</param>
    /// <param name="respostaRepository">Repositório de respostas de usuário.</param>
    /// <param name="pontuacaoRepository">Repositório de pontuação de usuário.</param>
    public RespostaService(
        IQuestaoService questaoService,
        IRespostaUsuarioRepository respostaRepository,
        IPontuacaoRepository pontuacaoRepository)
    {
        _questaoService = questaoService;
        _respostaRepository = respostaRepository;
        _pontuacaoRepository = pontuacaoRepository;
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
        // A checagem de que a questão existe e está aprovada já é feita pelo QuestaoService —
        // reaproveitada aqui em vez de duplicada.
        var questao = await _questaoService.ObterPorIdAsync(questaoId, cancellationToken);

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

        // Incremento atômico direto no Postgres (upsert), não ler->somar em memória->salvar — respostas
        // concorrentes do mesmo usuário não podem mais se sobrescrever (ver IPontuacaoRepository.IncrementarPontosAsync).
        // Troca consciente: diferente de AddAsync (só enfileirado, commitado junto com o resto no
        // SaveChangesAsync do orquestrador), este INSERT/UPDATE executa e commita imediatamente, fora
        // dessa transação — é o preço de ser atômico no banco em vez de no change tracker em memória.
        // Se o SaveChangesAsync do orquestrador falhar depois (RespostaUsuario/RevisaoEspacada), o ponto
        // já concedido não seria desfeito. Risco aceito: as únicas causas realistas de falha nesse ponto
        // (FK de usuario_id/questao_id) já foram validadas momentos antes nesta mesma chamada.
        var pontuacao = pontua
            ? await _pontuacaoRepository.IncrementarPontosAsync(usuarioId, pontosConcedidos, DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime), cancellationToken)
            : null;

        // RN-010/RN-011/RN-012: resposta é só enfileirada aqui (AddAsync) — quem persiste
        // (IUnitOfWork.SaveChangesAsync) é o orquestrador (RegistrarRespostaComEfeitosService), numa
        // única transação com a revisão espaçada. Pontuação já foi persistida acima, atomicamente.
        return new RegistrarRespostaResultado(resposta, questao, pontuacao ?? new Pontuacao { UsuarioId = usuarioId });
    }
}
