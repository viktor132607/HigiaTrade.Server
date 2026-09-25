using System.Data;
using System.Data.Common;
using System.Text.Json;
using HygiaTrade.API.Controllers;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IHomeSlideshowStore
{
    Task EnsureAsync();

    Task<HomeSlideshowPayload?> ReadAsync();

    Task WriteAsync(HomeSlideshowPayload payload);
}

public sealed class HomeSlideshowStore(
    ApplicationDbContext db,
    IHomeSlideshowDefaults defaults)
    : IHomeSlideshowStore
{
    private const string ContentKey =
        "home-hero-slideshow";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task EnsureAsync()
    {
        await WithConnectionAsync(
            async connection =>
            {
                await using DbCommand command =
                    connection.CreateCommand();

                command.CommandText = """
                    CREATE TABLE IF NOT EXISTS "SiteContent" (
                        "Key" text PRIMARY KEY,
                        "Value" jsonb NOT NULL,
                        "ModifiedOn" timestamp with time zone NOT NULL DEFAULT NOW()
                    );
                    """;

                await command.ExecuteNonQueryAsync();

                await using DbCommand seedCommand =
                    connection.CreateCommand();

                seedCommand.CommandText = """
                    INSERT INTO "SiteContent" ("Key", "Value", "ModifiedOn")
                    VALUES (@key, CAST(@value AS jsonb), NOW())
                    ON CONFLICT ("Key") DO NOTHING;
                    """;

                AddParameter(
                    seedCommand,
                    "@key",
                    ContentKey);

                AddParameter(
                    seedCommand,
                    "@value",
                    JsonSerializer.Serialize(
                        defaults.Create(),
                        JsonOptions));

                await seedCommand.ExecuteNonQueryAsync();
            });
    }

    public Task<HomeSlideshowPayload?> ReadAsync() =>
        WithConnectionAsync(
            async connection =>
            {
                await using DbCommand command =
                    connection.CreateCommand();

                command.CommandText =
                    "SELECT \"Value\"::text FROM \"SiteContent\" WHERE \"Key\" = @key LIMIT 1;";

                AddParameter(
                    command,
                    "@key",
                    ContentKey);

                object? result =
                    await command.ExecuteScalarAsync();

                string? json = result?.ToString();

                return string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize
                        <HomeSlideshowPayload>(
                            json,
                            JsonOptions);
            });

    public Task WriteAsync(
        HomeSlideshowPayload payload) =>
        WithConnectionAsync(
            async connection =>
            {
                await using DbCommand command =
                    connection.CreateCommand();

                command.CommandText = """
                    INSERT INTO "SiteContent" ("Key", "Value", "ModifiedOn")
                    VALUES (@key, CAST(@value AS jsonb), NOW())
                    ON CONFLICT ("Key") DO UPDATE SET
                        "Value" = EXCLUDED."Value",
                        "ModifiedOn" = NOW();
                    """;

                AddParameter(
                    command,
                    "@key",
                    ContentKey);

                AddParameter(
                    command,
                    "@value",
                    JsonSerializer.Serialize(
                        payload,
                        JsonOptions));

                await command.ExecuteNonQueryAsync();
            });

    private async Task WithConnectionAsync(
        Func<DbConnection, Task> action)
    {
        DbConnection connection =
            db.Database.GetDbConnection();

        bool closeAfter =
            connection.State != ConnectionState.Open;

        if (closeAfter)
        {
            await connection.OpenAsync();
        }

        try
        {
            await action(connection);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<T> WithConnectionAsync<T>(
        Func<DbConnection, Task<T>> action)
    {
        DbConnection connection =
            db.Database.GetDbConnection();

        bool closeAfter =
            connection.State != ConnectionState.Open;

        if (closeAfter)
        {
            await connection.OpenAsync();
        }

        try
        {
            return await action(connection);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(
        DbCommand command,
        string name,
        object value)
    {
        DbParameter parameter =
            command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
