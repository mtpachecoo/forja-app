namespace Forja.Domain.Questoes;

/// <summary>
/// Representa uma questão de estudo. Corresponde à tabela <c>questoes</c>.
/// </summary>
public class Questao
{
    /// <summary>Identificador único da questão.</summary>
    public Guid Id { get; set; }

    /// <summary>Identificador da carreira à qual a questão se refere.</summary>
    public Guid CarreiraId { get; set; }

    /// <summary>Identificador da banca relacionada, quando aplicável.</summary>
    public Guid? BancaId { get; set; }

    /// <summary>Identificador da disciplina à qual a questão pertence.</summary>
    public Guid DisciplinaId { get; set; }

    /// <summary>Identificador do tópico relacionado, quando aplicável.</summary>
    public Guid? TopicoId { get; set; }

    /// <summary>Formato da questão.</summary>
    public TipoQuestao Tipo { get; set; }

    /// <summary>Enunciado da questão.</summary>
    public string Enunciado { get; set; } = string.Empty;

    /// <summary>
    /// Alternativas da questão, aplicável apenas quando <see cref="Tipo"/> é
    /// <see cref="TipoQuestao.MultiplaEscolha"/>.
    /// </summary>
    public List<Alternativa>? Alternativas { get; set; }

    /// <summary>Gabarito da questão.</summary>
    public string Gabarito { get; set; } = string.Empty;

    /// <summary>Explicação/justificativa do gabarito.</summary>
    public string Explicacao { get; set; } = string.Empty;

    /// <summary>Identificadores dos chunks de conteúdo usados como fonte da questão.</summary>
    public Guid[] FontesChunkIds { get; set; } = [];

    /// <summary>Status de revisão editorial da questão.</summary>
    public StatusQuestao Status { get; set; } = StatusQuestao.Rascunho;

    /// <summary>Identificador do usuário que revisou a questão, quando aplicável.</summary>
    public Guid? RevisadoPor { get; set; }

    /// <summary>Data e hora em que a questão foi revisada, quando aplicável.</summary>
    public DateTimeOffset? RevisadoEm { get; set; }

    /// <summary>Data e hora de criação do registro.</summary>
    public DateTimeOffset CriadoEm { get; set; }

    /// <summary>Origem de criação da questão.</summary>
    public OrigemQuestao Origem { get; set; } = OrigemQuestao.ReproduzidaProvaOficial;

    /// <summary>Identificador da fonte de conteúdo do tipo prova de origem, quando aplicável.</summary>
    public Guid? FonteProvaId { get; set; }

    /// <summary>Ano da prova de origem, quando aplicável.</summary>
    public int? Ano { get; set; }

    /// <summary>Órgão da prova de origem, quando aplicável.</summary>
    public string? Orgao { get; set; }
}
