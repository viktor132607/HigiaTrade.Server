using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HygiaTrade.API.Models;

namespace HygiaTrade.API.Services;

public sealed record ParsedInvoice(
    string? InvoiceNumber,
    string? InvoiceDate,
    string DetectedLanguage,
    IReadOnlyList<ExtractedInvoiceItem> Items);

public interface IInvoiceParser
{
    ParsedInvoice Parse(
        string text,
        IReadOnlyList<InvoiceCatalogProduct> catalog);
}

public sealed partial class InvoiceParser : IInvoiceParser
{
    private static readonly string[] RejectLineTerms =
    [
        "subtotal", "междинна сума", "total", "общо", "vat", "ддс", "tax", "данък",
        "tax base", "данъчна основа", "net amount", "grand total", "amount due", "currency",
        "payment", "плащане", "bank", "банка", "iban", "swift", "address", "адрес",
        "customer", "клиент", "supplier", "доставчик", "получател", "основание",
        "document", "created only", "not valid for accounting", "не е валидна за счетоводни цели"
    ];

    private sealed record PreparedCatalogProduct(
        Guid Id,
        string Title,
        string NormalizedTitle,
        string[] Tokens);

    public ParsedInvoice Parse(
        string text,
        IReadOnlyList<InvoiceCatalogProduct> catalog)
    {
        PreparedCatalogProduct[] preparedCatalog = catalog
            .Select(product =>
            {
                string normalized = NormalizeForMatch(product.Title);
                return new PreparedCatalogProduct(
                    product.Id,
                    product.Title,
                    normalized,
                    Tokenize(normalized));
            })
            .Where(product => product.Tokens.Length > 0)
            .ToArray();

        string? invoiceNumber = FindInvoiceNumber(text);
        string? invoiceDate = FindInvoiceDate(text);

        IReadOnlyList<ExtractedInvoiceItem> items = ParseItems(
            text,
            preparedCatalog,
            invoiceNumber,
            invoiceDate);

        return new ParsedInvoice(
            invoiceNumber,
            invoiceDate,
            DetectLanguage(text),
            items);
    }

    private static IReadOnlyList<ExtractedInvoiceItem> ParseItems(
        string text,
        IReadOnlyList<PreparedCatalogProduct> catalog,
        string? invoiceNumber,
        string? invoiceDate)
    {
        string[] lines = text
            .Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(line =>
                Regex.Replace(line, @"[\t ]+", " ").Trim())
            .Where(line => line.Length >= 4)
            .Take(800)
            .ToArray();

        var parsed = new List<ExtractedInvoiceItem>();

        foreach (string line in lines)
        {
            if (!IsPotentialItemLine(
                    line,
                    invoiceNumber,
                    invoiceDate))
            {
                continue;
            }

            string normalizedLine = NormalizeForMatch(line);

            var scored = catalog
                .Select(product => new
                {
                    Product = product,
                    Score = ScoreMatch(product, normalizedLine)
                })
                .Where(item => item.Score >= 0.30)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Product.Title.Length)
                .Take(5)
                .ToArray();

            var best = scored.FirstOrDefault();

            decimal quantity = ExtractQuantity(
                line,
                best?.Product.Title,
                out double quantityConfidence);

            if (quantity <= 0 ||
                quantity != decimal.Truncate(quantity))
            {
                continue;
            }

            string rawName =
                ExtractProductName(line, best?.Product.Title);

            if (rawName.Length < 3)
            {
                continue;
            }

            if (best is null &&
                !LooksLikeNumberedItem(line) &&
                !QuantityUnitRegex().IsMatch(line))
            {
                continue;
            }

            ProductCandidate[] candidates = scored
                .Select(item => new ProductCandidate(
                    item.Product.Id,
                    item.Product.Title,
                    Math.Round(item.Score, 3)))
                .ToArray();

            parsed.Add(new ExtractedInvoiceItem(
                rawName,
                quantity,
                best?.Score >= 0.58
                    ? best.Product.Id
                    : null,
                best?.Score >= 0.58
                    ? best.Product.Title
                    : null,
                best is null
                    ? 0
                    : Math.Round(best.Score, 3),
                quantityConfidence,
                candidates,
                line));
        }

        return parsed
            .GroupBy(
                item => NormalizeForMatch(item.RawName),
                StringComparer.Ordinal)
            .Select(group => group.First())
            .Take(200)
            .ToArray();
    }

    private static bool IsPotentialItemLine(
        string line,
        string? invoiceNumber,
        string? invoiceDate)
    {
        string normalized = NormalizeForMatch(line);

        if (RejectLineTerms.Any(term =>
                normalized.Contains(
                    NormalizeForMatch(term),
                    StringComparison.Ordinal)))
        {
            return false;
        }

        if (normalized.StartsWith(
                "date ",
                StringComparison.Ordinal) ||
            normalized.StartsWith(
                "дата ",
                StringComparison.Ordinal))
        {
            return false;
        }

        if (normalized.StartsWith(
                "invoice ",
                StringComparison.Ordinal) ||
            normalized.StartsWith(
                "фактура ",
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(invoiceNumber) &&
            line.Contains(
                invoiceNumber,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(invoiceDate) &&
            normalized.Length < 40 &&
            line.Contains(
                invoiceDate,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (DateOnlyLineRegex().IsMatch(line))
        {
            return false;
        }

        if (!line.Any(char.IsLetter))
        {
            return false;
        }

        return NumberRegex().IsMatch(line);
    }

    private static bool LooksLikeNumberedItem(string line) =>
        Regex.IsMatch(
            line,
            @"^\s*\d{1,3}[\s.)-]+\p{L}");

    private static double ScoreMatch(
        PreparedCatalogProduct product,
        string normalizedLine)
    {
        if (normalizedLine.Length == 0)
        {
            return 0;
        }

        if (normalizedLine.Contains(
                product.NormalizedTitle,
                StringComparison.Ordinal))
        {
            return 0.99;
        }

        HashSet<string> lineTokens = Tokenize(normalizedLine)
            .ToHashSet(StringComparer.Ordinal);

        if (lineTokens.Count == 0)
        {
            return 0;
        }

        int matchedTokens =
            product.Tokens.Count(lineTokens.Contains);

        double coverage =
            matchedTokens / (double)product.Tokens.Length;

        int union = product.Tokens
            .Union(lineTokens, StringComparer.Ordinal)
            .Count();

        double jaccard =
            union == 0
                ? 0
                : matchedTokens / (double)union;

        string[] importantTokens = product.Tokens
            .Where(token => token.Length >= 4)
            .ToArray();

        double importantMatched =
            importantTokens.Length == 0
                ? coverage
                : importantTokens.Count(lineTokens.Contains) /
                  (double)importantTokens.Length;

        return Math.Min(
            0.98,
            (coverage * 0.58) +
            (importantMatched * 0.30) +
            (jaccard * 0.12));
    }

    private static decimal ExtractQuantity(
        string line,
        string? productTitle,
        out double confidence)
    {
        confidence = 0;

        Match unitMatch = QuantityUnitRegex().Match(line);
        if (unitMatch.Success &&
            TryParseDecimal(
                unitMatch.Groups[1].Value,
                out decimal unitQty) &&
            unitQty > 0)
        {
            confidence = 0.98;
            return unitQty;
        }

        string working = Regex.Replace(
            line,
            @"^\s*\d{1,3}[\s.)-]+",
            string.Empty);

        HashSet<string> productNumbers =
            string.IsNullOrWhiteSpace(productTitle)
                ? new HashSet<string>()
                : NumberRegex()
                    .Matches(productTitle)
                    .Select(match =>
                        NormalizeNumberToken(match.Value))
                    .ToHashSet(StringComparer.Ordinal);

        foreach (Match match in NumberRegex().Matches(working))
        {
            if (match.Value.Contains('%'))
            {
                continue;
            }

            if (match.Value.Contains('.') ||
                match.Value.Contains(','))
            {
                continue;
            }

            if (productNumbers.Contains(
                    NormalizeNumberToken(match.Value)))
            {
                continue;
            }

            if (!TryParseDecimal(
                    match.Value,
                    out decimal value) ||
                value <= 0 ||
                value > 100000)
            {
                continue;
            }

            confidence = 0.62;
            return value;
        }

        return 0;
    }

    private static string ExtractProductName(
        string line,
        string? matchedTitle)
    {
        string cleaned = Regex.Replace(
                line,
                @"^\s*\d{1,3}[\s.)-]+",
                string.Empty)
            .Trim();

        Match unitMatch = QuantityUnitRegex().Match(cleaned);

        if (unitMatch.Success && unitMatch.Index >= 3)
        {
            cleaned = cleaned[..unitMatch.Index];
        }
        else
        {
            cleaned = Regex.Replace(
                cleaned,
                @"\s+\d+\s+(?:EUR|BGN|лв\.?|€).*$",
                string.Empty,
                RegexOptions.IgnoreCase);

            cleaned = Regex.Replace(
                cleaned,
                @"\s+\d+(?:[.,]\d{2})\s+(?:EUR|BGN|лв\.?|€).*$",
                string.Empty,
                RegexOptions.IgnoreCase);
        }

        cleaned = Regex.Replace(
                cleaned,
                @"\s+",
                " ")
            .Trim(
                ' ',
                '-',
                '|',
                ':',
                '›',
                '>');

        if (cleaned.Length < 3 &&
            !string.IsNullOrWhiteSpace(matchedTitle))
        {
            return matchedTitle;
        }

        return cleaned.Length > 220
            ? cleaned[..220]
            : cleaned;
    }

    private static string? FindInvoiceNumber(string text)
    {
        foreach (Regex regex in new[]
                 {
                     InvoiceNumberRegex(),
                     InvoiceNumberMarkerRegex(),
                     InvoiceNumberLooseRegex()
                 })
        {
            Match match = regex.Match(text);
            if (!match.Success)
            {
                continue;
            }

            string value = match.Groups[1].Value
                .Trim()
                .Trim(':', '-', '#', '№');

            if (value.Length is < 2 or > 100)
            {
                continue;
            }

            if (!value.Any(char.IsDigit))
            {
                continue;
            }

            if (!value.Any(char.IsLetterOrDigit))
            {
                continue;
            }

            return value;
        }

        return null;
    }

    private static string? FindInvoiceDate(string text)
    {
        Match labeled = LabeledDateRegex().Match(text);
        if (labeled.Success)
        {
            return labeled.Groups[1].Value;
        }

        Match match = DateRegex().Match(text);
        return match.Success
            ? match.Value
            : null;
    }

    private static string DetectLanguage(string text)
    {
        int cyrillic = text.Count(character =>
            character is >= '\u0400' and <= '\u04FF');

        int latin = text.Count(character =>
            character is >= 'A' and <= 'z');

        return cyrillic > Math.Max(8, latin / 5)
            ? "bg"
            : "en";
    }

    private static string NormalizeForMatch(string value)
    {
        var builder = new StringBuilder(value.Length);
        bool previousSpace = false;

        foreach (char character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousSpace = false;
            }
            else if (!previousSpace)
            {
                builder.Append(' ');
                previousSpace = true;
            }
        }

        return builder.ToString().Trim();
    }

    private static string[] Tokenize(string normalized) =>
        normalized
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string NormalizeNumberToken(string value) =>
        value
            .Replace(" ", string.Empty)
            .Replace(',', '.');

    private static bool TryParseDecimal(
        string value,
        out decimal result)
    {
        string cleaned = value
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("%", string.Empty);

        if (cleaned.Contains(',') &&
            cleaned.Contains('.'))
        {
            int lastComma = cleaned.LastIndexOf(',');
            int lastDot = cleaned.LastIndexOf('.');

            cleaned = lastComma > lastDot
                ? cleaned
                    .Replace(".", string.Empty)
                    .Replace(',', '.')
                : cleaned.Replace(",", string.Empty);
        }
        else if (cleaned.Contains(','))
        {
            cleaned = cleaned.Replace(',', '.');
        }

        return decimal.TryParse(
            cleaned,
            NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out result);
    }

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}])[-+]?\d{1,6}(?:[\s.,]\d{3})*(?:[.,]\d+)?%?",
        RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();

    [GeneratedRegex(
        @"(?i)(?<!\p{L})(\d{1,6})\s*(?:бр\.?|броя|pcs?\.?|pieces?|qty\.?|x)(?!\p{L})",
        RegexOptions.CultureInvariant)]
    private static partial Regex QuantityUnitRegex();

    [GeneratedRegex(
        @"(?im)(?:invoice|фактура)\s*(?:no\.?|number|номер|№|#)?\s*[:\-]?\s*(?:no\.?|number|номер|№|#)?\s*[:\-]?\s*([\p{L}\p{N}][\p{L}\p{N}_\/\-.]{1,99})",
        RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberRegex();

    [GeneratedRegex(
        @"(?im)(?:^|\s)(?:№|#|no\.?|номер)\s*[:\-]?\s*([\p{L}\p{N}][\p{L}\p{N}_\/\-.]{1,99})",
        RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberMarkerRegex();

    [GeneratedRegex(
        @"(?im)\b([A-ZА-Я]{1,12}(?:[-_/][A-ZА-Я0-9]{1,20}){1,8})\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberLooseRegex();

    [GeneratedRegex(
        @"(?im)(?:date|дата)\s*[:\-]?\s*((?:0?[1-9]|[12]\d|3[01])[.\-/](?:0?[1-9]|1[0-2])[.\-/](?:20)?\d{2})",
        RegexOptions.CultureInvariant)]
    private static partial Regex LabeledDateRegex();

    [GeneratedRegex(
        @"\b(?:0?[1-9]|[12]\d|3[01])[.\-/](?:0?[1-9]|1[0-2])[.\-/](?:20)?\d{2}\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex DateRegex();

    [GeneratedRegex(
        @"^\s*(?:date|дата)?\s*[:\-]?\s*(?:0?[1-9]|[12]\d|3[01])[.\-/](?:0?[1-9]|1[0-2])[.\-/](?:20)?\d{2}\s*$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant)]
    private static partial Regex DateOnlyLineRegex();
}
