using System.Data;
using System.Globalization;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public sealed record InvoiceCatalogProduct(
    Guid Id,
    string Title);

public interface IInvoiceImportRepository
{
    Task<IReadOnlyList<InvoiceCatalogProduct>> GetCatalogAsync(
        CancellationToken cancellationToken);

    Task<bool> InvoiceAlreadyImportedAsync(
        string invoiceNumber,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task ApplyStockImportAsync(
        IReadOnlyList<Product> products,
        IReadOnlyDictionary<Guid, int> quantities,
        string invoiceNumber,
        DateTime createdOn,
        CancellationToken cancellationToken);
}

public sealed class InvoiceImportRepository(
    ApplicationDbContext db) : IInvoiceImportRepository
{
    public async Task<IReadOnlyList<InvoiceCatalogProduct>> GetCatalogAsync(
        CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .OrderBy(product => product.Title)
            .Select(product => new InvoiceCatalogProduct(
                product.Id,
                product.Title))
            .ToListAsync(cancellationToken);

    public async Task<bool> InvoiceAlreadyImportedAsync(
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        bool shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM \"StockEntries\" WHERE LOWER(\"InvoiceNumber\") = LOWER(@invoiceNumber)";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@invoiceNumber";
            parameter.Value = invoiceNumber.Trim();
            command.Parameters.Add(parameter);

            object? value =
                await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt64(
                value,
                CultureInfo.InvariantCulture) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        await db.Products
            .Where(product =>
                productIds.Contains(product.Id) &&
                !product.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task ApplyStockImportAsync(
        IReadOnlyList<Product> products,
        IReadOnlyDictionary<Guid, int> quantities,
        string invoiceNumber,
        DateTime createdOn,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (Product product in products)
            {
                int additionalQuantity = quantities[product.Id];
                product.Quantity += (uint)additionalQuantity;
                product.ModifiedOn = createdOn;
            }

            await db.SaveChangesAsync(cancellationToken);

            foreach (Product product in products)
            {
                Guid entryId = Guid.NewGuid();
                int quantity = quantities[product.Id];

                await db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ""StockEntries"" (""Id"", ""ProductId"", ""Quantity"", ""InvoiceNumber"", ""CreatedOn"")
                    VALUES ({entryId}, {product.Id}, {quantity}, {invoiceNumber}, {createdOn})",
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
