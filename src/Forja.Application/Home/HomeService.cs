using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;

namespace Forja.Application.Home;

/// <summary>
/// Implementação padrão de <see cref="IHomeService"/>.
/// </summary>
public class HomeService : IHomeService
{
    private readonly IStreakRepository _streakRepository;
    private readonly IPontuacaoRepository _pontuacaoRepository;
    private readonly IPlanoEstudoRepository _planoEstudoRepository;
    private readonly IPlanoItemRepository _planoItemRepository;
    private readonly IRespostaUsuarioRepository _respostaUsuarioRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public HomeService(
        IStreakRepository streakRepository,
        IPontuacaoRepository pontuacaoRepository,
        IPlanoEstudoRepository planoEstudoRepository,
        IPlanoItemRepository planoItemRepository,
        IRespostaUsuarioRepository respostaUsuarioRepository)
    {
        _streakRepository = streakRepository;
        _pontuacaoRepository = pontuacaoRepository;
        _planoEstudoRepository = planoEstudoRepository;
        _planoItemRepository = planoItemRepository;
        _respostaUsuarioRepository = respostaUsuarioRepository;
    }

    /// <inheritdoc />
    public async Task<ResumoHome> ObterResumoAsync(Guid usuarioId, int quantidadeAtividades, CancellationToken cancellationToken = default)
    {
        var streak = await _streakRepository.GetByIdAsync(usuarioId, cancellationToken);
        var pontuacao = await _pontuacaoRepository.GetByIdAsync(usuarioId, cancellationToken);
        var percentualPlanoConcluido = await ObterPercentualPlanoConcluidoAsync(usuarioId, cancellationToken);

        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var respostasHoje = await _respostaUsuarioRepository.ContarRespostasNoDiaAsync(usuarioId, hoje, cancellationToken);
        var ultimasRespostas = await _respostaUsuarioRepository.GetUltimasAsync(usuarioId, quantidadeAtividades, cancellationToken);

        return new ResumoHome(
            streak?.DiasConsecutivos ?? 0,
            pontuacao?.PontosTotal ?? 0,
            pontuacao?.PontosSemanaAtual ?? 0,
            percentualPlanoConcluido,
            respostasHoje,
            ultimasRespostas.Select(r => new AtividadeRecente(r.QuestaoId, r.Correta, r.CriadoEm)).ToList());
    }

    private async Task<decimal?> ObterPercentualPlanoConcluidoAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        // Plano mais recente do usuário entre todas as carreiras — Home não é escopada por carreira,
        // é um resumo geral, e a Home nunca gera plano (isso é o onboarding/RF-002): se não existe
        // nenhum ainda, o percentual fica null em vez de forçar uma geração como efeito colateral.
        var planos = await _planoEstudoRepository.GetByUsuarioIdAsync(usuarioId, cancellationToken);
        var planoMaisRecente = planos.OrderByDescending(p => p.CriadoEm).FirstOrDefault();
        if (planoMaisRecente is null)
        {
            return null;
        }

        var itens = await _planoItemRepository.GetByPlanoIdAsync(planoMaisRecente.Id, cancellationToken);
        if (itens.Count == 0)
        {
            return null;
        }

        var concluidos = itens.Count(i => i.Status == StatusItemPlano.Concluido);
        return Math.Round(100m * concluidos / itens.Count, 2);
    }
}
