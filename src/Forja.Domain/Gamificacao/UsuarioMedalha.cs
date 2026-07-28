namespace Forja.Domain.Gamificacao;

/// <summary>
/// Representa a conquista de uma medalha por um usuário. Corresponde à tabela <c>usuario_medalhas</c>.
/// Chave primária composta por <see cref="UsuarioId"/> e <see cref="MedalhaId"/>.
/// </summary>
public class UsuarioMedalha
{
    /// <summary>Identificador do usuário que conquistou a medalha.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Identificador da medalha conquistada.</summary>
    public Guid MedalhaId { get; set; }

    /// <summary>Data e hora em que a medalha foi conquistada.</summary>
    public DateTimeOffset ConquistadaEm { get; set; }
}
