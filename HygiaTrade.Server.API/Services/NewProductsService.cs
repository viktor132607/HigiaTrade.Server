using System.Data.Common;
using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Data;
using HygiaTrade.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface INewProductsService
{
    Task<NewProductsPageResponse> GetAsync(
        int pageNumber,
        int pageSize);

    Task<NewProductStatusDto> GetStatusAsync(Guid productId);

    Task<NewProductStatusDto> UpdateStatusAsync(
        Guid productId,
        UpdateNewProductStatusRequest request);
}

public sealed class NewProductsServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class NewProductsService(
    ApplicationDbContext db,
    IProductService productService) : INewProductsService
{
    public async Task<NewProductsPageResponse> GetAsync(
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        List<Guid> activeProductIds = [];

        await db.Database.OpenConnectionAsync();

        try
        {
            await using DbCommand command =
                db.Database.GetDbConnection().CreateCommand();

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
                activeProductIds.Add(reader.GetGuid(0));
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        int totalCount = activeProductIds.Count;

        IEnumerable<Guid> pageIds = activeProductIds
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        List<object> products = [];

        foreach (Guid productId in pageIds)
        {
            ProductResponse? product =
                await productService.GetByIdAsync(productId);

            if (product is not null && product.IsActive)
            {
                products.Add(product);
            }
        }

        return new NewProductsPageResponse(
            products,
            totalCount);
    }

    public async Task<NewProductStatusDto> GetStatusAsync(
        Guid productId)
    {
        return await ReadStatusAsync(productId);
    }

    public async Task<NewProductStatusDto> UpdateStatusAsync(
        Guid productId,
        UpdateNewProductStatusRequest request)
    {
        bool productExists = await db.Products
            .AsNoTracking()
            .AnyAsync(product =>
                product.Id == productId &&
                !product.IsDeleted);

        if (!productExists)
        {
            throw new NewProductsServiceException(
                StatusCodes.Status404NotFound,
                "Product not found.");
        }

        if (!request.IsNewProduct)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"ProductNewStatuses\" WHERE \"ProductId\" = {productId}");

            return new NewProductStatusDto(
                false,
                14,
                null,
                false);
        }

        if (request.DisplayDays is < 1 or > 365)
        {
            throw new NewProductsServiceException(
                StatusCodes.Status400BadRequest,
                "DisplayDays must be between 1 and 365.");
        }

        DateTime activeUntilUtc =
            DateTime.UtcNow.AddDays(request.DisplayDays);

        await db.Database.OpenConnectionAsync();

        try
        {
            await using DbCommand command =
                db.Database.GetDbConnection().CreateCommand();

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
                request.DisplayDays);

            AddParameter(
                command,
                "@activeUntilUtc",
                activeUntilUtc);

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        return await ReadStatusAsync(productId);
    }

    private async Task<NewProductStatusDto> ReadStatusAsync(
        Guid productId)
    {
        await db.Database.OpenConnectionAsync();

        try
        {
            await using DbCommand command =
                db.Database.GetDbConnection().CreateCommand();

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
                return new NewProductStatusDto(
                    false,
                    14,
                    null,
                    false);
            }

            int displayDays = reader.GetInt32(0);
            DateTime activeUntilUtc = reader.GetDateTime(1);

            bool isCurrentlyNew =
                activeUntilUtc > DateTime.UtcNow;

            return new NewProductStatusDto(
                true,
                displayDays,
                activeUntilUtc,
                isCurrentlyNew);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
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
