using System.Data;
using System.Data.Common;
using System.Globalization;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IInvoiceNumberLookup
{
    Task<bool> ExistsAsync(
        string normalizedInvoiceNumber,
        CancellationToken cancellationToken);
}

public sealed class InvoiceNumberLookup(
    ApplicationDbContext db)
    : IInvoiceNumberLookup
{
    public async Task<bool> ExistsAsync(
        string normalizedInvoiceNumber,
        CancellationToken cancellationToken)
    {
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

            command.CommandText =
                "SELECT COUNT(*) FROM \"StockEntries\" WHERE LOWER(\"InvoiceNumber\") = LOWER(@invoiceNumber)";

            DbParameter parameter =
                command.CreateParameter();

            parameter.ParameterName =
                "@invoiceNumber";

            parameter.Value =
                normalizedInvoiceNumber;

            command.Parameters.Add(parameter);

            object? value =
                await command.ExecuteScalarAsync(
                    cancellationToken);

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
}
