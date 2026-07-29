using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Usuarios;

namespace Forja.Application.Gamificacao;

/// <summary>
/// Implementação padrão de <see cref="IPontuacaoService"/>.
/// </summary>
public class PontuacaoService : IPontuacaoService
{
    private readonly IPontuacaoRepository _pontuacaoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPlanoEstudoRepository _planoEstudoRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public PontuacaoService(
        IPontuacaoRepository pontuacaoRepository,
        IUsuarioRepository usuarioRepository,
        IPlanoEstudoRepository planoEstudoRepository)
    {
        _pontuacaoRepository = pontuacaoRepository;
        _usuarioRepository = usuarioRepository;
        _planoEstudoRepository = planoEstudoRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RankingItem>> ObterRankingSemanalAsync(Guid? carreiraId, CancellationToken cancellationToken = default)
    {
        var inicioSemanaAtual = Pontuacao.InicioDaSemana(DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime));
        var pontuacoes = await _pontuacaoRepository.GetRankingSemanalAsync(inicioSemanaAtual, cancellationToken);

        if (carreiraId.HasValue)
        {
            var planos = await _planoEstudoRepository.GetByCarreiraIdAsync(carreiraId.Value, cancellationToken);
            var usuarioIdsDaCarreira = planos.Select(p => p.UsuarioId).ToHashSet();
            pontuacoes = pontuacoes.Where(p => usuarioIdsDaCarreira.Contains(p.UsuarioId)).ToList();
        }

        var usuarios = await _usuarioRepository.GetByIdsAsync(pontuacoes.Select(p => p.UsuarioId), cancellationToken);
        var nomePorUsuarioId = usuarios.ToDictionary(u => u.Id, u => u.Nome);

        return pontuacoes
            .Select((p, indice) => new RankingItem(
                p.UsuarioId,
                nomePorUsuarioId.GetValueOrDefault(p.UsuarioId, "Usuário"),
                p.PontosSemanaAtual,
                indice + 1))
            .ToList();
    }
}
