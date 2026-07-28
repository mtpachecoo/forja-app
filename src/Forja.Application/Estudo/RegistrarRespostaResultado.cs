using Forja.Domain.Estudo;
using Forja.Domain.Gamificacao;
using Forja.Domain.Questoes;

namespace Forja.Application.Estudo;

/// <summary>
/// Resultado do registro de uma resposta, com o estado de pontuação do usuário após a operação.
/// </summary>
/// <param name="Resposta">A resposta registrada.</param>
/// <param name="Questao">A questão respondida (para expor gabarito e explicação após a resposta).</param>
/// <param name="Pontuacao">A pontuação atual do usuário, já refletindo esta resposta.</param>
public sealed record RegistrarRespostaResultado(RespostaUsuario Resposta, Questao Questao, Pontuacao Pontuacao);
