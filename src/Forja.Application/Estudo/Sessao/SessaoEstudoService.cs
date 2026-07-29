using Forja.Domain.Estudo;

namespace Forja.Application.Estudo;

/// <summary>
/// Implementação padrão de <see cref="ISessaoEstudoService"/>.
/// </summary>
public class SessaoEstudoService : ISessaoEstudoService
{
    private readonly ISessaoEstudoRepository _sessaoEstudoRepository;

    /// <summary>
    /// Cria uma nova instância do serviço.
    /// </summary>
    public SessaoEstudoService(ISessaoEstudoRepository sessaoEstudoRepository)
    {
        _sessaoEstudoRepository = sessaoEstudoRepository;
    }

    /// <inheritdoc />
    public async Task<SessaoEstudo> IniciarSessaoAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var sessoesDeHoje = await _sessaoEstudoRepository.GetByUsuarioIdEDataAsync(usuarioId, hoje, cancellationToken);

        var sessaoExistente = sessoesDeHoje.FirstOrDefault();
        if (sessaoExistente is not null)
        {
            return sessaoExistente;
        }

        var sessao = new SessaoEstudo
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            DataSessao = hoje,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await _sessaoEstudoRepository.AddAsync(sessao, cancellationToken);

        // Quem persiste (IUnitOfWork.SaveChangesAsync) é o orquestrador
        // (IniciarSessaoComEfeitosService), numa única transação com o streak.
        return sessao;
    }
}
