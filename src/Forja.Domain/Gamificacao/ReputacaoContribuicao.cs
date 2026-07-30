namespace Forja.Domain.Gamificacao;

/// <summary>
/// Reputação de um usuário por contribuições de conteúdo aprovadas — distinta de <see cref="Pontuacao"/>,
/// que mede atividade de estudo (responder questões), não contribuição. Corresponde à tabela
/// <c>reputacao_contribuicao</c>. Chave primária é <see cref="UsuarioId"/>, mesmo formato de
/// <see cref="Pontuacao"/>/<see cref="Streak"/>.
/// </summary>
public class ReputacaoContribuicao
{
    /// <summary>Identificador do usuário. Chave primária da tabela.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Pontos de reputação acumulados por contribuições aprovadas.</summary>
    public int PontosContribuicao { get; set; }
}
