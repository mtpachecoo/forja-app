namespace Forja.Domain.Usuarios;

/// <summary>
/// Representa os dados básicos de identidade de um usuário vindos de um provedor de autenticação
/// externo, usados para provisionar o registro em <see cref="Usuario"/> no primeiro acesso.
/// </summary>
/// <param name="Id">Identificador do usuário no provedor externo (mesmo valor de <see cref="Usuario.Id"/>).</param>
/// <param name="Nome">Nome do usuário cadastrado no provedor externo.</param>
/// <param name="Email">E-mail do usuário cadastrado no provedor externo.</param>
public sealed record IdentidadeExterna(Guid Id, string Nome, string Email);
