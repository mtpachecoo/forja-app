namespace Forja.Api;

/// <summary>
/// Corpo da requisição de <c>POST /onboarding</c>. O identificador do usuário nunca vem do cliente —
/// é resolvido a partir do token autenticado.
/// </summary>
/// <param name="CarreiraId">Carreira escolhida para o plano de estudo.</param>
/// <param name="TempoDisponivelMinDia">Tempo disponível por dia de estudo, em minutos.</param>
/// <param name="Nivel">
/// Nível de conhecimento declarado, como texto (<c>"Iniciante"</c>, <c>"Intermediario"</c> ou
/// <c>"Avancado"</c>, sem diferenciar maiúsculas/minúsculas) — validado no endpoint.
/// </param>
public sealed record OnboardingRequest(Guid CarreiraId, int TempoDisponivelMinDia, string Nivel);
