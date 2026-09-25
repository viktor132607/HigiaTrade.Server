using System.Data;
using System.Data.Common;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public sealed record NewProductStatusRecord(
    int DisplayDays,
    DateTime ActiveUntilUtc);

public interface INewProductStatusRepository
{
    Task<IReadOnlyList<Guid>> GetActiveProductIdsAsync();

    Task<bool> ProductExistsAsync(Guid productId);

    Task<NewProductStatusRecord?> ReadAsync(
        Guid productId);

    Task DeleteAsync(Guid productId);

    Task UpsertAsync(
        Guid productId,
        int displayDays,
        DateTime activeUntilUtc);
}

public sealed class NewProductStatusRepository(
    ApplicationDbContext db)
    : INewProductStatusRepository
{
    public async Task<IReadOnlyList<Guid>>
        GetActiveProductIdsAsync()
    {
        List<Guid> result = [];

        await WithConnectionAsync(
            async connection =>
            {
                await using DbCommand command =
                    connection.CreateCommand();

                command.CommandText = """
                    SELECT n."ProductId"
                    FROM "ProductNewStatuses" n
                    INNER JOIN "Products" p ON p."Id" = n."ProductId"
                    WHERE n."ActiveUntilUtc" > NOW()
                      AND p."IsDeleted" = FALSE
                      AND p."IsActive" = TRUE
                    ORDER BY n."ActiveUntilUtc" DESC;
                    """;

                await using DbDataReader reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(
                        reader.GetGuid(0));
                }
            });

        return result;
    }

    public Task<bool> ProductExistsAsync(
        Guid productId) =>
        db.Products
            .AsNoTracking()
            .AnyAsync(product =>
                product.Id == productId &&
                !product.IsDeleted);

    public Task<NewProductStatusRecord?> ReadAsync(
        Guid productId) =>
        WithConnectionAsync(
            async connection =>
            {
                await using DbCommand command =
                    connection.CreateCommand();

                command.CommandText = """
                    SELECT "DisplayDays", "ActiveUntilUtc"
                    FROM "ProductNewStatuses"
                    WHERE "ProductId" = @productId
                    LIMIT 1;
                    """;

                AddParameter(
                    command,
                    "@productId",
                    productId);

                await using DbDataReader reader =
                    await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return null;
                }

                return new NewProductStatusRecord(
                    reader.GetInt32(0),
                    reader.GetDateTime(1));
            });

    public Task DeleteAsync(
        Guid productId) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"ProductNewStatuses\" WHERE \"ProductId\" = {productId}");

    public Task UpsertAsync(
        Guid productId,
        int displayDays,
        DateTime activeUntilUtc) =>
        WithConnectionAsync(
            async connection =>
            {
                await using DbCommand command =
                    connection.CreateCommand();

                command.CommandText = """
                    INSERT INTO "ProductNewStatuses" ("ProductId", "DisplayDays", "ActiveUntilUtc")
                    VALUES (@productId, @displayDays, @activeUntilUtc)
                    ON CONFLICT ("ProductId") DO UPDATE SET
                        "DisplayDays" = EXCLUDED."DisplayDays",
                        "ActiveUntilUtc" = CASE
                            WHEN "ProductNewStatuses"."DisplayDays" <> EXCLUDED."DisplayDays"
                              OR "ProductNewStatuses"."ActiveUntilUtc" <= NOW()
                            THEN EXCLUDED."ActiveUntilUtc"
                            ELSE "ProductNewStatuses"."ActiveUntilUtc"
                        END;
                    """;

                AddParameter(
                    command,
                    "@productId",
                    productId);

                AddParameter(
                    command,
                    "@displayDays",
                    displayDays);

                AddParameter(
                    command,
                    "@activeUntilUtc",
                    activeUntilUtc);

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
