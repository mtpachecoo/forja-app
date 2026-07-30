using Forja.Domain.Gamificacao;

namespace Forja.Domain.Usuarios;

/// <summary>
/// Projeção read-only e explícita do perfil público de um usuário — deliberadamente <b>não</b> estende
/// <see cref="Common.IRepository{TEntity,TKey}"/>: o ponto aqui é uma leitura já achatada e paginação
/// pública, não CRUD de <see cref="Usuario"/>. Só os campos seguros pra exposição pública passam por
/// aqui; nunca <see cref="Usuario.Email"/> ou qualquer outro dado privado.
/// </summary>
public interface IPerfilPublicoReadStore
{
    /// <summary>
    /// Obtém o perfil público de um usuário.
    /// </summary>
    /// <param name="usuarioId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>O perfil público, ou <c>null</c> se o usuário não existe.</returns>
    Task<PerfilPublico?> ObterPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Perfil público de um usuário — campo fechado, deliberadamente sem <c>avatar</c> (não há coluna
/// correspondente em <see cref="Usuario"/> hoje) nem qualquer outro campo de <see cref="Usuario"/> além
/// do nome. Ver <c>PerfilPublicoProjecaoTests</c> (Forja.Infrastructure.Tests) para a trava que impede
/// um campo novo vazar aqui sem querer.
/// </summary>
/// <param name="UsuarioId">Identificador do usuário.</param>
/// <param name="Nome">Nome do usuário.</param>
/// <param name="Badges">Nomes das medalhas conquistadas.</param>
/// <param name="ContribuicoesAprovadas">Quantidade de contribuições de conteúdo aprovadas.</param>
/// <param name="NivelReputacao">Nível de reputação calculado (<see cref="ReputacaoCalculator"/>).</param>
public sealed record PerfilPublico(
    Guid UsuarioId,
    string Nome,
    IReadOnlyList<string> Badges,
    int ContribuicoesAprovadas,
    NivelReputacao NivelReputacao);
