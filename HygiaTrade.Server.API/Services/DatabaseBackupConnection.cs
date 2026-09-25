using Npgsql;

namespace HygiaTrade.API.Services;

public sealed class DatabaseBackupConnection
{
    private readonly NpgsqlConnectionStringBuilder connection;

    public DatabaseBackupConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        connection =
            new NpgsqlConnectionStringBuilder(
                connectionString);

        if (string.IsNullOrWhiteSpace(connection.Host) ||
            string.IsNullOrWhiteSpace(connection.Database) ||
            string.IsNullOrWhiteSpace(connection.Username))
        {
            throw new InvalidOperationException(
                "Database backup requires PostgreSQL host, database, and username configuration.");
        }
    }

    public string Database => connection.Database!;

    public IReadOnlyDictionary<string, string>
        CreateEnvironment()
    {
        var environment =
            new Dictionary<string, string>
            {
                ["PGHOST"] = connection.Host,
                ["PGPORT"] = connection.Port.ToString(),
                ["PGDATABASE"] = connection.Database!,
                ["PGUSER"] = connection.Username!,
                ["PGCONNECT_TIMEOUT"] = "20",
                ["PGAPPNAME"] =
                    "higiatrade-database-backup",
                ["PGSSLMODE"] =
                    ResolveSslMode(connection.SslMode.ToString())
            };

        if (!string.IsNullOrEmpty(connection.Password))
        {
            environment["PGPASSWORD"] =
                connection.Password;
        }

        return environment;
    }

    private static string ResolveSslMode(string sslMode) =>
        sslMode switch
        {
            "VerifyCA" => "verify-ca",
            "VerifyFull" => "verify-full",
            _ => sslMode.ToLowerInvariant()
        };
}
