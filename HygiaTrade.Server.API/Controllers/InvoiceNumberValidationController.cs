using System.Data;
using System.Globalization;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/InvoiceImport")]
public sealed class InvoiceNumberValidationController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("check-number")]
    public async Task<IActionResult> CheckInvoiceNumberAsync([FromQuery] string invoiceNumber, CancellationToken cancellationToken)
    {
        var normalized = invoiceNumber?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return Ok(new { exists = false });
        }

        if (normalized.Length > 100)
        {
            return BadRequest(new { message = "Invoice number cannot exceed 100 characters." });
        }

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM \"StockEntries\" WHERE LOWER(\"InvoiceNumber\") = LOWER(@invoiceNumber)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@invoiceNumber";
            parameter.Value = normalized;
            command.Parameters.Add(parameter);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            var exists = Convert.ToInt64(value, CultureInfo.InvariantCulture) > 0;
            return Ok(new { exists });
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }
}
