namespace Forja.Infrastructure.Tests;

/// <summary>
/// Carrega variáveis de um arquivo <c>.env</c> (formato <c>chave=valor</c>) no processo atual,
/// para uso exclusivo dos testes que precisam de credenciais locais (ex.: conexão real com o Neon).
/// </summary>
internal static class EnvFile
{
    /// <summary>
    /// Carrega o arquivo <c>.env</c> localizado na raiz do repositório, se existir.
    /// Variáveis já definidas no ambiente não são sobrescritas.
    /// </summary>
    public static void Load()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, ".env")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            return;
        }

        var path = Path.Combine(directory.FullName, ".env");
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
