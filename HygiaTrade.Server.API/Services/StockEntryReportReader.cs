using System.Data;
using System.Data.Common;
using HygiaTrade.API.Models;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IStockEntryReportReader
{
    Task<IReadOnlyList<StockEntryRow>> ReadSafeAsync(
        DateTime fromUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken);
}

public sealed class StockEntryReportReader(
    ApplicationDbContext db,
    ILogger<StockEntryReportReader> logger)
    : IStockEntryReportReader
{
    public async Task<IReadOnlyList<StockEntryRow>>
        ReadSafeAsync(
            DateTime fromUtc,
            DateTime toExclusiveUtc,
            CancellationToken cancellationToken)
    {
        try
        {
            return await ReadAsync(
                fromUtc,
                toExclusiveUtc,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "StockEntries could not be read. The rest of the report will still be returned.");

            return [];
        }
    }

    private async Task<IReadOnlyList<StockEntryRow>>
        ReadAsync(
            DateTime fromUtc,
            DateTime toExclusiveUtc,
            CancellationToken cancellationToken)
    {
        List<StockEntryRow> result = [];

        DbConnection connection =
            db.Database.GetDbConnection();

        bool shouldClose =
            connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(
                cancellationToken);
        }

        try
        {
            await using DbCommand command =
                connection.CreateCommand();

            command.CommandText = """
                SELECT
                    s."Id",
                    s."ProductId",
                    p."Title",
                    COALESCE(c."Name", 'Без категория'),
                    s."Quantity",
                    s."InvoiceNumber",
                    s."CreatedOn"
                FROM "StockEntries" s
                INNER JOIN "Products" p ON p."Id" = s."ProductId"
                LEFT JOIN "Categories" c ON c."Id" = p."CategoryId"
                WHERE p."IsDeleted" = FALSE
                  AND s."CreatedOn" >= @fromUtc
                  AND s."CreatedOn" < @toExclusiveUtc
                ORDER BY s."CreatedOn" DESC
                """;

            AddParameter(
                command,
                "@fromUtc",
                fromUtc);

            AddParameter(
                command,
                "@toExclusiveUtc",
                toExclusiveUtc);

            await using DbDataReader reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            while (await reader.ReadAsync(
                cancellationToken))
            {
                result.Add(
                    new StockEntryRow(
                        reader.GetGuid(0),
                        reader.GetGuid(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetInt32(4),
                        reader.GetString(5),
                        reader.GetDateTime(6)));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return result;
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
