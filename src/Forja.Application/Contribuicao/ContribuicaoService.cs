using Forja.Application.Common;
using Forja.Domain.Catalogo;
using Forja.Domain.Common;
using Forja.Domain.Contribuicao;
using Forja.Domain.Gamificacao;
using Forja.Domain.Usuarios;

namespace Forja.Application.Contribuicao;

/// <summary>
/// Implementação padrão de <see cref="IContribuicaoService"/>.
/// </summary>
public class ContribuicaoService : IContribuicaoService
{
    /// <summary>
    /// Pontos de reputação concedidos por contribuição aprovada — calibração inicial, sem RN
    /// preexistente pra herdar (mecânica nova nesta fase); ajustar se se mostrar rápida/lenta demais.
    /// </summary>
    private const int PontosPorContribuicaoAprovada = 10;

    private readonly IContribuicaoConteudoRepository _contribuicaoRepository;
    private readonly ITopicoRepository _topicoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IReputacaoContribuicaoRepository _reputacaoRepository;
    private readonly IUsuarioMedalhaRepository _usuarioMedalhaRepository;
    private readonly IAdminAuthorizer _adminAuthorizer;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public ContribuicaoService(
        IContribuicaoConteudoRepository contribuicaoRepository,
        ITopicoRepository topicoRepository,
        IUsuarioRepository usuarioRepository,
        IReputacaoContribuicaoRepository reputacaoRepository,
        IUsuarioMedalhaRepository usuarioMedalhaRepository,
        IAdminAuthorizer adminAuthorizer,
        IUnitOfWork unitOfWork)
    {
        _contribuicaoRepository = contribuicaoRepository;
        _topicoRepository = topicoRepository;
        _usuarioRepository = usuarioRepository;
        _reputacaoRepository = reputacaoRepository;
        _usuarioMedalhaRepository = usuarioMedalhaRepository;
        _adminAuthorizer = adminAuthorizer;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ContribuicaoConteudo> SubmeterAsync(
        Guid usuarioId, Guid topicoId, TipoContribuicao tipo, string link, CancellationToken cancellationToken = default)
    {
        _ = await _topicoRepository.GetByIdAsync(topicoId, cancellationToken)
            ?? throw new NotFoundException("Tópico", topicoId);

        if (!Uri.TryCreate(link, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Link '{link}' não é uma URL válida.");
        }

        var contribuicao = new ContribuicaoConteudo
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TopicoId = topicoId,
            Tipo = tipo,
            Link = link,
            Status = StatusContribuicao.EmRevisao,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await _contribuicaoRepository.AddAsync(contribuicao, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return contribuicao;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ContribuicaoConteudo>> ListarAprovadasPorTopicoAsync(
        Guid topicoId, int skip, int take, CancellationToken cancellationToken = default)
        => _contribuicaoRepository.GetAprovadasPorTopicoAsync(topicoId, skip, take, cancellationToken);

    /// <inheritdoc />
    public async Task<ContribuicaoConteudo> AprovarAsync(Guid contribuicaoId, Guid moderadorUsuarioId, CancellationToken cancellationToken = default)
    {
        var contribuicao = await ModerarAsync(contribuicaoId, moderadorUsuarioId, StatusContribuicao.Aprovada, cancellationToken);

        var reputacao = await _reputacaoRepository.GetByIdAsync(contribuicao.UsuarioId, cancellationToken);
        if (reputacao is null)
        {
            // AddAsync já deixa a entidade rastreada como "Added" — chamar Update() nela em seguida
            // (como fazíamos antes) reclassificava o estado pra "Modified", e o EF gerava um UPDATE
            // pra uma linha que ainda não existe, falhando com 0 linhas afetadas.
            reputacao = new ReputacaoContribuicao { UsuarioId = contribuicao.UsuarioId, PontosContribuicao = PontosPorContribuicaoAprovada };
            await _reputacaoRepository.AddAsync(reputacao, cancellationToken);
        }
        else
        {
            reputacao.PontosContribuicao += PontosPorContribuicaoAprovada;
            _reputacaoRepository.Update(reputacao);
        }

        var jaTemMedalha = await _usuarioMedalhaRepository.ExisteAsync(
            contribuicao.UsuarioId, MedalhasConhecidas.PrimeiraContribuicaoAprovadaId, cancellationToken);
        if (!jaTemMedalha)
        {
            await _usuarioMedalhaRepository.AddAsync(new UsuarioMedalha
            {
                UsuarioId = contribuicao.UsuarioId,
                MedalhaId = MedalhasConhecidas.PrimeiraContribuicaoAprovadaId,
                ConquistadaEm = DateTimeOffset.UtcNow,
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return contribuicao;
    }

    /// <inheritdoc />
    public async Task<ContribuicaoConteudo> RejeitarAsync(Guid contribuicaoId, Guid moderadorUsuarioId, CancellationToken cancellationToken = default)
    {
        var contribuicao = await ModerarAsync(contribuicaoId, moderadorUsuarioId, StatusContribuicao.Rejeitada, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return contribuicao;
    }

    private async Task<ContribuicaoConteudo> ModerarAsync(
        Guid contribuicaoId, Guid moderadorUsuarioId, StatusContribuicao novoStatus, CancellationToken cancellationToken)
    {
        var moderador = await _usuarioRepository.GetByIdAsync(moderadorUsuarioId, cancellationToken)
            ?? throw new NotFoundException("Usuário", moderadorUsuarioId);
        if (!_adminAuthorizer.EhAdmin(moderador.Email))
        {
            throw new NaoAutorizadoException("Usuário não tem permissão para moderar contribuições.");
        }

        var contribuicao = await _contribuicaoRepository.GetByIdAsync(contribuicaoId, cancellationToken)
            ?? throw new NotFoundException("Contribuição", contribuicaoId);

        contribuicao.Status = novoStatus;
        contribuicao.ModeradoPor = moderadorUsuarioId;
        contribuicao.ModeradoEm = DateTimeOffset.UtcNow;
        _contribuicaoRepository.Update(contribuicao);

        return contribuicao;
    }
}
