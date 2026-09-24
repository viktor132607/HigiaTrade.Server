using System.Data;
using System.Globalization;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IInvoiceNumberService
{
    Task<InvoiceNumberStatus> CheckAsync(
        string? invoiceNumber,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        string invoiceNumber,
        CancellationToken cancellationToken);
}

public sealed record InvoiceNumberStatus(
    bool Exists);

public sealed class InvoiceNumberServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class InvoiceNumberService(
    ApplicationDbContext db) : IInvoiceNumberService
{
    public async Task<InvoiceNumberStatus> CheckAsync(
        string? invoiceNumber,
        CancellationToken cancellationToken)
    {
        string normalized =
            Normalize(invoiceNumber);

        if (normalized.Length == 0)
        {
            return new InvoiceNumberStatus(false);
        }

        ValidateLength(normalized);

        bool exists =
            await ExistsNormalizedAsync(
                normalized,
                cancellationToken);

        return new InvoiceNumberStatus(exists);
    }

    public async Task<bool> ExistsAsync(
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        string normalized =
            Normalize(invoiceNumber);

        if (normalized.Length == 0)
        {
            return false;
        }

        ValidateLength(normalized);

        return await ExistsNormalizedAsync(
            normalized,
            cancellationToken);
    }

    private async Task<bool> ExistsNormalizedAsync(
        string normalized,
        CancellationToken cancellationToken)
    {
        var connection =
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
            await using var command =
                connection.CreateCommand();

            command.CommandText =
                "SELECT COUNT(*) FROM \"StockEntries\" WHERE LOWER(\"InvoiceNumber\") = LOWER(@invoiceNumber)";

            var parameter =
                command.CreateParameter();

            parameter.ParameterName =
                "@invoiceNumber";

            parameter.Value =
                normalized;

            command.Parameters.Add(
                parameter);

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

    private static string Normalize(
        string? invoiceNumber)
    {
        return invoiceNumber?.Trim() ??
               string.Empty;
    }

    private static void ValidateLength(
        string normalized)
    {
        if (normalized.Length > 100)
        {
            throw new InvoiceNumberServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number cannot exceed 100 characters.");
        }
    }
}
