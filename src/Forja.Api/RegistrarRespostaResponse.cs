namespace Forja.Api;

/// <summary>
/// Resposta de <c>POST /respostas</c>. Correção e pontuação são sempre calculadas no backend (RN-004).
/// </summary>
/// <param name="Correta">Indica se a resposta está correta.</param>
/// <param name="Pontuada">Indica se esta resposta gerou pontuação.</param>
/// <param name="PontosConcedidos">Pontos concedidos por esta resposta especificamente.</param>
/// <param name="PontosTotal">Pontuação total acumulada do usuário após esta resposta.</param>
/// <param name="PontosSemanaAtual">Pontuação acumulada na semana atual após esta resposta.</param>
/// <param name="Gabarito">Gabarito da questão, revelado agora que o usuário já respondeu.</param>
/// <param name="Explicacao">Explicação/justificativa do gabarito.</param>
public sealed record RegistrarRespostaResponse(
    bool Correta,
    bool Pontuada,
    int PontosConcedidos,
    int PontosTotal,
    int PontosSemanaAtual,
    string Gabarito,
    string Explicacao);
