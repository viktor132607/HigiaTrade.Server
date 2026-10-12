using System.Globalization;

namespace HygiaTrade.API.Services;

public sealed record ProductBulkCommand(decimal? Price, uint? Quantity);

// Only explicitly registered application commands are accepted; never executes shell or SQL input.
public static class AdminCommandParser
{
    public static ProductBulkCommand Parse(string? command)
    {
        if (string.IsNullOrWhiteSpace(command) || command.Length > 200)
            throw new ArgumentException("Enter a command of up to 200 characters.");

        var parts = command.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if ((parts.Length != 4 && parts.Length != 6) ||
            !parts[0].Equals("/products", StringComparison.OrdinalIgnoreCase) ||
            !parts[1].Equals("all", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Usage: /products all price <EUR> | quantity <count> | price <EUR> quantity <count>");

        decimal? price = null;
        uint? quantity = null;
        for (var i = 2; i < parts.Length; i += 2)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "price" when price is null:
                    if (!decimal.TryParse(parts[i + 1].Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture, out var parsedPrice) || parsedPrice < 0 ||
                        parsedPrice > 9999999999999999.99m || decimal.Round(parsedPrice, 2) != parsedPrice)
                        throw new ArgumentException("Price must be a non-negative EUR amount with at most two decimal places.");
                    price = parsedPrice;
                    break;
                case "quantity" when quantity is null:
                    if (!uint.TryParse(parts[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedQuantity) ||
                        parsedQuantity > int.MaxValue)
                        throw new ArgumentException("Quantity must be a whole number between 0 and 2147483647.");
                    quantity = parsedQuantity;
                    break;
                default:
                    throw new ArgumentException("Unknown or repeated parameter. Available parameters: price, quantity.");
            }
        }
        return new ProductBulkCommand(price, quantity);
    }
}
