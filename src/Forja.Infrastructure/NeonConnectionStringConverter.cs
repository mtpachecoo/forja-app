namespace Forja.Infrastructure;

/// <summary>
/// Converte a connection string do Neon, fornecida no formato URI
/// (<c>postgresql://usuario:senha@host/banco?parametros</c>), para o formato
/// chave=valor nativo do Npgsql.
/// </summary>
public static class NeonConnectionStringConverter
{
    /// <summary>
    /// Converte uma connection string no formato URI do Postgres para o formato Npgsql.
    /// </summary>
    /// <param name="uriConnectionString">Connection string no formato <c>postgresql://usuario:senha@host/banco?parametros</c>.</param>
    /// <returns>Connection string no formato chave=valor aceito pelo Npgsql.</returns>
    public static string ToNpgsqlConnectionString(string uriConnectionString)
    {
        var uri = new Uri(uriConnectionString);

        var userInfoParts = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfoParts[0]);
        var password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        var query = ParseQuery(uri.Query);

        var builder = new System.Text.StringBuilder();
        builder.Append("Host=").Append(uri.Host).Append(';');
        builder.Append("Port=").Append(port).Append(';');
        builder.Append("Username=").Append(username).Append(';');
        builder.Append("Password=").Append(password).Append(';');
        builder.Append("Database=").Append(database).Append(';');

        if (query.TryGetValue("sslmode", out var sslMode) && !string.IsNullOrEmpty(sslMode))
        {
            builder.Append("SSL Mode=").Append(sslMode).Append(';');
        }

        if (query.TryGetValue("channel_binding", out var channelBinding) && !string.IsNullOrEmpty(channelBinding))
        {
            builder.Append("Channel Binding=").Append(channelBinding).Append(';');
        }

        return builder.ToString();
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.TrimStart('?');
        if (trimmed.Length == 0)
        {
            return result;
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
