using System.Globalization;

namespace Forja.Integration.Tests.Performance;

/// <summary>
/// Coleta amostras de tempo (ms) e calcula p50/p95/p99, anexando os resultados a um arquivo markdown
/// de relatório compartilhado entre as classes de teste de carga (Onboarding, Plano, Sessão, Pontuação).
/// Uso exclusivo desta rodada de auditoria de performance — não faz parte da suíte funcional.
/// </summary>
internal static class PerformanceReport
{
    /// <summary>
    /// Caminho do relatório consolidado, fora do repositório. Configurável via
    /// <c>FORJA_PERF_REPORT_PATH</c> (usado para apontar pro scratchpad da sessão); cai pra um arquivo
    /// temporário padrão se não definida.
    /// </summary>
    public static string CaminhoArquivo { get; } =
        Environment.GetEnvironmentVariable("FORJA_PERF_REPORT_PATH")
        ?? Path.Combine(Path.GetTempPath(), "forja-performance-report.md");

    public static void EscreverSecao(string titulo, IReadOnlyList<double> amostrasMs, string? notaAdicional = null)
    {
        var (p50, p95, p99) = CalcularPercentis(amostrasMs);
        var linhas = new List<string>
        {
            $"## {titulo}",
            "",
            $"- Amostras: {amostrasMs.Count}",
            $"- p50: {p50.ToString("F1", CultureInfo.InvariantCulture)} ms",
            $"- p95: {p95.ToString("F1", CultureInfo.InvariantCulture)} ms",
            $"- p99: {p99.ToString("F1", CultureInfo.InvariantCulture)} ms",
            $"- min/max: {amostrasMs.Min().ToString("F1", CultureInfo.InvariantCulture)} ms / {amostrasMs.Max().ToString("F1", CultureInfo.InvariantCulture)} ms",
        };

        if (notaAdicional is not null)
        {
            linhas.Add($"- {notaAdicional}");
        }

        linhas.Add("");

        File.AppendAllLines(CaminhoArquivo, linhas);
    }

    public static void EscreverLinha(string linha) => File.AppendAllLines(CaminhoArquivo, [linha, ""]);

    private static (double P50, double P95, double P99) CalcularPercentis(IReadOnlyList<double> amostras)
    {
        var ordenadas = amostras.OrderBy(x => x).ToList();
        return (Percentil(ordenadas, 0.50), Percentil(ordenadas, 0.95), Percentil(ordenadas, 0.99));
    }

    private static double Percentil(IReadOnlyList<double> ordenadas, double p)
    {
        if (ordenadas.Count == 1)
        {
            return ordenadas[0];
        }

        var indice = p * (ordenadas.Count - 1);
        var indiceBaixo = (int)Math.Floor(indice);
        var indiceAlto = (int)Math.Ceiling(indice);
        if (indiceBaixo == indiceAlto)
        {
            return ordenadas[indiceBaixo];
        }

        var fracao = indice - indiceBaixo;
        return ordenadas[indiceBaixo] * (1 - fracao) + ordenadas[indiceAlto] * fracao;
    }
}
