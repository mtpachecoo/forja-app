using Forja.Application.Onboarding;

namespace Forja.Api;

/// <summary>Resposta de <c>POST /onboarding</c>.</summary>
/// <param name="Nivel">Nível de conhecimento salvo.</param>
/// <param name="TempoDisponivelMinDia">Tempo disponível por dia de estudo salvo, em minutos.</param>
/// <param name="Plano">Plano de estudo gerado (ou reaproveitado) com o contexto informado.</param>
public sealed record OnboardingResponse(string Nivel, int TempoDisponivelMinDia, PlanoAtualResponse Plano)
{
    /// <summary>Constrói a resposta a partir do resultado do serviço.</summary>
    public static OnboardingResponse De(OnboardingResultado resultado) => new(
        resultado.Usuario.Nivel.ToString(),
        resultado.Usuario.TempoDisponivelMinDia,
        PlanoAtualResponse.De(resultado.Plano));
}
