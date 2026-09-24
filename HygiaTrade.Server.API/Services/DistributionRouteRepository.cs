using System.Data;
using System.Data.Common;
using System.Text.Json;
using HygiaTrade.API.Controllers;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IDistributionRouteRepository
{
    Task<DistributionRouteStore> GetStoreAsync(
        CancellationToken cancellationToken);

    Task SaveStoreAsync(
        DistributionRouteStore store,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetActiveOrdersAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetOrdersAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken);
}

public sealed class DistributionRouteRepository(
    ApplicationDbContext db) : IDistributionRouteRepository
{
    private const string ContentKey = "distribution-routes";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<DistributionRouteStore> GetStoreAsync(
        CancellationToken cancellationToken)
    {
        await EnsureStoreAsync(cancellationToken);

        return await ReadAsync(cancellationToken)
            ?? new DistributionRouteStore();
    }

    public Task<IReadOnlyList<Order>> GetActiveOrdersAsync(
        CancellationToken cancellationToken) =>
        QueryActiveOrdersAsync(cancellationToken);

    public Task<IReadOnlyList<Order>> GetOrdersAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken) =>
        QueryOrdersAsync(orderIds, cancellationToken);

    public async Task SaveStoreAsync(
        DistributionRouteStore store,
        CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter =
            connection.State != ConnectionState.Open;

        if (closeAfter)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
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

            AddParameter(command, "@key", ContentKey);
            AddParameter(
                command,
                "@value",
                JsonSerializer.Serialize(store, JsonOptions));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<IReadOnlyList<Order>> QueryActiveOrdersAsync(
        CancellationToken cancellationToken) =>
        await db.Orders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted &&
                order.Status != OrderStatus.Delivered &&
                order.Status != OrderStatus.Cancelled)
            .OrderBy(order => order.CreatedOn)
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<Order>> QueryOrdersAsync(
        IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken) =>
        await db.Orders
            .AsNoTracking()
            .Where(order =>
                orderIds.Contains(order.Id) &&
                !order.IsDeleted)
            .ToListAsync(cancellationToken);

    private async Task EnsureStoreAsync(
        CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter =
            connection.State != ConnectionState.Open;

        if (closeAfter)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
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

            await command.ExecuteNonQueryAsync(cancellationToken);

            await using DbCommand seedCommand =
                connection.CreateCommand();

            seedCommand.CommandText = """
                INSERT INTO "SiteContent" ("Key", "Value", "ModifiedOn")
                VALUES (@key, CAST(@value AS jsonb), NOW())
                ON CONFLICT ("Key") DO NOTHING;
                """;

            AddParameter(seedCommand, "@key", ContentKey);
            AddParameter(
                seedCommand,
                "@value",
                JsonSerializer.Serialize(
                    new DistributionRouteStore(),
                    JsonOptions));

            await seedCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<DistributionRouteStore?> ReadAsync(
        CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter =
            connection.State != ConnectionState.Open;

        if (closeAfter)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command =
                connection.CreateCommand();

            command.CommandText =
                "SELECT \"Value\"::text FROM \"SiteContent\" WHERE \"Key\" = @key LIMIT 1;";

            AddParameter(command, "@key", ContentKey);

            object? result =
                await command.ExecuteScalarAsync(cancellationToken);

            string? json = result?.ToString();

            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<DistributionRouteStore>(
                    json,
                    JsonOptions);
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
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
