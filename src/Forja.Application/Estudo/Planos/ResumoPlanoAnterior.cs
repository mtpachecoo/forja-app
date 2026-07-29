namespace Forja.Application.Estudo;

/// <summary>
/// Retrato do plano de estudo anterior no momento em que ele é substituído por um novo
/// (<c>POST /plano/recriar</c>) — o que valeria "anotar" antes de seguir em frente.
/// </summary>
/// <param name="TotalTopicos">Quantidade total de tópicos que o plano anterior tinha.</param>
/// <param name="TopicosConcluidos">Quantidade de tópicos já concluídos no plano anterior.</param>
/// <param name="DiasRestantesAteProva">
/// Dias restantes até a data da prova do edital atual da carreira, ou <c>null</c> se a data da prova
/// ainda não foi divulgada.
/// </param>
public sealed record ResumoPlanoAnterior(int TotalTopicos, int TopicosConcluidos, int? DiasRestantesAteProva);
