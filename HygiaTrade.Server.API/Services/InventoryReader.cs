using System.Data;
using System.Data.Common;
using HygiaTrade.API.Controllers;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IInventoryReader
{
    Task<uint?> GetProductQuantityAsync(
        Guid productId);

    Task<IReadOnlyList<StockEntryResponse>>
        GetEntriesAsync(Guid productId);
}

public sealed class InventoryReader(
    ApplicationDbContext db)
    : IInventoryReader
{
    public async Task<uint?> GetProductQuantityAsync(
        Guid productId)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(product =>
                product.Id == productId &&
                !product.IsDeleted)
            .Select(product => new
            {
                product.Quantity
            })
            .FirstOrDefaultAsync();

        return product?.Quantity;
    }

    public async Task<IReadOnlyList<StockEntryResponse>>
        GetEntriesAsync(Guid productId)
    {
        List<StockEntryResponse> entries = [];

        DbConnection connection =
            db.Database.GetDbConnection();

        bool shouldClose =
            connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using DbCommand command =
                connection.CreateCommand();

            command.CommandText =
                "SELECT \"Id\", \"Quantity\", \"InvoiceNumber\", \"CreatedOn\" " +
                "FROM \"StockEntries\" WHERE \"ProductId\" = @productId " +
                "ORDER BY \"CreatedOn\" DESC";

            AddParameter(
                command,
                "@productId",
                productId);

            await using DbDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entries.Add(
                    new StockEntryResponse(
                        reader.GetGuid(0),
                        reader.GetInt32(1),
                        reader.GetString(2),
                        reader.GetDateTime(3)));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return entries;
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
