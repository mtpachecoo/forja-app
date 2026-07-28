namespace Forja.Api;

/// <summary>
/// Perfil do usuário autenticado, retornado por <c>GET /me</c>.
/// </summary>
/// <param name="Id">Identificador do usuário.</param>
/// <param name="Nome">Nome do usuário.</param>
/// <param name="Email">E-mail do usuário.</param>
/// <param name="Nivel">Nível de conhecimento declarado.</param>
/// <param name="TempoDisponivelMinDia">Tempo disponível por dia para estudo, em minutos.</param>
/// <param name="FusoHorario">Fuso horário do usuário.</param>
public sealed record UsuarioPerfilResponse(
    Guid Id,
    string Nome,
    string Email,
    string Nivel,
    int TempoDisponivelMinDia,
    string FusoHorario);
