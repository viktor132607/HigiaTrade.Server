using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public partial class InvoiceImportController(ApplicationDbContext db) : ControllerBase
{
    private const long MaxFileSize = 15 * 1024 * 1024;
    private const int MaxPdfPages = 20;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp"
    };

    private static readonly string[] IgnoreLineTerms =
    [
        "invoice", "фактура", "subtotal", "междинна сума", "total", "общо", "vat", "ддс",
        "tax", "данък", "payment", "плащане", "bank", "банка", "iban", "swift", "address",
        "адрес", "customer", "клиент", "supplier", "доставчик", "description quantity", "описание количество"
    ];

    public sealed record ProductCandidate(Guid Id, string Name, double Confidence);

    public sealed record ExtractedInvoiceItem(
        string RawName,
        decimal Quantity,
        Guid? MatchedProductId,
        string? MatchedProductName,
        double MatchConfidence,
        double QuantityConfidence,
        IReadOnlyList<ProductCandidate> Candidates,
        string SourceLine);

    public sealed record ExtractInvoiceResponse(
        string FileName,
        string DetectedLanguage,
        string? InvoiceNumber,
        string? InvoiceDate,
        bool DuplicateInvoice,
        IReadOnlyList<ExtractedInvoiceItem> Items,
        string TextPreview);

    public sealed record ImportInvoiceItem(Guid ProductId, int Quantity);
    public sealed record ImportInvoiceRequest(string InvoiceNumber, IReadOnlyList<ImportInvoiceItem> Items);

    private sealed record CatalogProduct(Guid Id, string Title, string NormalizedTitle, string[] Tokens);
    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

    [HttpPost("extract")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> ExtractAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Choose an invoice file." });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "The invoice file cannot exceed 15 MB." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Supported formats are PDF, PNG, JPG, JPEG and WEBP." });
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "hygiatrade-invoices", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var inputPath = Path.Combine(tempRoot, $"invoice{extension.ToLowerInvariant()}");

        try
        {
            await using (var stream = System.IO.File.Create(inputPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var extractedText = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? await ExtractPdfTextAsync(inputPath, tempRoot, cancellationToken)
                : await OcrImageAsync(inputPath, cancellationToken);

            extractedText = NormalizeExtractedText(extractedText);
            if (CountLetters(extractedText) < 8)
            {
                return UnprocessableEntity(new
                {
                    message = "No readable invoice text was detected. Try a sharper image or a higher-quality PDF."
                });
            }

            var catalog = await db.Products
                .AsNoTracking()
                .Where(product => !product.IsDeleted)
                .OrderBy(product => product.Title)
                .Select(product => new { product.Id, product.Title })
                .ToListAsync(cancellationToken);

            var preparedCatalog = catalog
                .Select(product =>
                {
                    var normalized = NormalizeForMatch(product.Title);
                    return new CatalogProduct(product.Id, product.Title, normalized, Tokenize(normalized));
                })
                .Where(product => product.Tokens.Length > 0)
                .ToArray();

            var items = ParseItems(extractedText, preparedCatalog);
            var invoiceNumber = FindInvoiceNumber(extractedText);
            var invoiceDate = FindInvoiceDate(extractedText);
            var detectedLanguage = DetectLanguage(extractedText);
            var duplicateInvoice = invoiceNumber is not null && await InvoiceAlreadyImportedAsync(invoiceNumber, cancellationToken);

            return Ok(new ExtractInvoiceResponse(
                file.FileName,
                detectedLanguage,
                invoiceNumber,
                invoiceDate,
                duplicateInvoice,
                items,
                extractedText.Length > 5000 ? extractedText[..5000] : extractedText));
        }
        catch (FileNotFoundException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = exception.Message
            });
        }
        catch (TimeoutException exception)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { message = exception.Message });
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
            catch
            {
                // Temporary cleanup should never break the request response.
            }
        }
    }

    [HttpPost("commit")]
    public async Task<IActionResult> CommitAsync([FromBody] ImportInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoiceNumber = request.InvoiceNumber?.Trim() ?? string.Empty;
        if (invoiceNumber.Length == 0)
        {
            return BadRequest(new { message = "Invoice number is required before importing stock." });
        }

        if (invoiceNumber.Length > 100)
        {
            return BadRequest(new { message = "Invoice number cannot exceed 100 characters." });
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "Choose at least one matched product to import." });
        }

        if (request.Items.Count > 200)
        {
            return BadRequest(new { message = "A single invoice can import up to 200 product rows." });
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return BadRequest(new { message = "Every imported quantity must be greater than zero." });
        }

        if (await InvoiceAlreadyImportedAsync(invoiceNumber, cancellationToken))
        {
            return Conflict(new { message = "This invoice number already has stock entries and cannot be imported twice." });
        }

        var aggregated = request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var productIds = aggregated.Keys.ToArray();
        var products = await db.Products
            .Where(product => productIds.Contains(product.Id) && !product.IsDeleted)
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Length)
        {
            return BadRequest(new { message = "One or more selected products no longer exist." });
        }

        foreach (var product in products)
        {
            var additionalQuantity = aggregated[product.Id];
            if ((ulong)product.Quantity + (ulong)additionalQuantity > uint.MaxValue)
            {
                return BadRequest(new { message = $"The resulting quantity for '{product.Title}' is too large." });
            }
        }

        var createdOn = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var product in products)
            {
                var additionalQuantity = aggregated[product.Id];
                product.Quantity += (uint)additionalQuantity;
                product.ModifiedOn = createdOn;
            }

            await db.SaveChangesAsync(cancellationToken);

            foreach (var product in products)
            {
                var entryId = Guid.NewGuid();
                var quantity = aggregated[product.Id];
                await db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ""StockEntries"" (""Id"", ""ProductId"", ""Quantity"", ""InvoiceNumber"", ""CreatedOn"")
                    VALUES ({entryId}, {product.Id}, {quantity}, {invoiceNumber}, {createdOn})", cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Ok(new
        {
            invoiceNumber,
            importedProducts = products.Count,
            importedUnits = aggregated.Values.Sum(),
            items = products.Select(product => new
            {
                product.Id,
                product.Title,
                addedQuantity = aggregated[product.Id],
                currentQuantity = product.Quantity
            })
        });
    }

    private async Task<string> ExtractPdfTextAsync(string inputPath, string tempRoot, CancellationToken cancellationToken)
    {
        var digitalText = await RunProcessAsync(
            "pdftotext",
            ["-layout", "-enc", "UTF-8", inputPath, "-"],
            TimeSpan.FromSeconds(25),
            cancellationToken);

        if (digitalText.ExitCode == 0 && CountLetters(digitalText.StdOut) >= 40)
        {
            return digitalText.StdOut;
        }

        var pagePrefix = Path.Combine(tempRoot, "page");
        var render = await RunProcessAsync(
            "pdftoppm",
            ["-png", "-r", "180", "-f", "1", "-l", MaxPdfPages.ToString(CultureInfo.InvariantCulture), inputPath, pagePrefix],
            TimeSpan.FromSeconds(90),
            cancellationToken);

        if (render.ExitCode != 0)
        {
            throw new InvalidOperationException($"The PDF could not be rendered for OCR: {render.StdErr.Trim()}");
        }

        var pageFiles = Directory.GetFiles(tempRoot, "page-*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (pageFiles.Length == 0)
        {
            throw new InvalidOperationException("The PDF did not contain readable pages.");
        }

        var builder = new StringBuilder();
        foreach (var pageFile in pageFiles)
        {
            builder.AppendLine(await OcrImageAsync(pageFile, cancellationToken));
        }

        return builder.ToString();
    }

    private async Task<string> OcrImageAsync(string inputPath, CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(
            "tesseract",
            [inputPath, "stdout", "-l", "bul+eng", "--oem", "1", "--psm", "4", "-c", "preserve_interword_spaces=1"],
            TimeSpan.FromMinutes(3),
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"OCR failed: {result.StdErr.Trim()}");
        }

        return result.StdOut;
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new FileNotFoundException($"Required OCR executable '{executable}' is not available on the server.");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new FileNotFoundException($"Required OCR executable '{executable}' is not available on the server.");
        }

        using (process)
        using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeoutCts.CancelAfter(timeout);
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // Best effort cleanup.
                }

                throw new TimeoutException($"Invoice processing exceeded the {timeout.TotalSeconds:0}-second limit.");
            }

            return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
        }
    }

    private static IReadOnlyList<ExtractedInvoiceItem> ParseItems(string text, IReadOnlyList<CatalogProduct> catalog)
    {
        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => Regex.Replace(line, @"[\t ]+", " ").Trim())
            .Where(line => line.Length >= 4)
            .Where(IsPotentialItemLine)
            .Take(600)
            .ToArray();

        var parsed = new List<ExtractedInvoiceItem>();

        foreach (var line in lines)
        {
            var normalizedLine = NormalizeForMatch(line);
            var scored = catalog
                .Select(product => new
                {
                    Product = product,
                    Score = ScoreMatch(product, normalizedLine)
                })
                .Where(item => item.Score >= 0.48)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Product.Title.Length)
                .Take(3)
                .ToArray();

            var best = scored.FirstOrDefault();
            var quantity = ExtractQuantity(line, best?.Product.Title, out var quantityConfidence);

            if (best is null)
            {
                var rawName = ExtractRawDescription(line);
                if (rawName.Length < 3 || quantity <= 0)
                {
                    continue;
                }

                parsed.Add(new ExtractedInvoiceItem(
                    rawName,
                    quantity,
                    null,
                    null,
                    0,
                    quantityConfidence,
                    [],
                    line));
                continue;
            }

            var candidates = scored
                .Select(item => new ProductCandidate(item.Product.Id, item.Product.Title, Math.Round(item.Score, 3)))
                .ToArray();

            parsed.Add(new ExtractedInvoiceItem(
                best.Product.Title,
                quantity > 0 ? quantity : 1,
                best.Score >= 0.62 ? best.Product.Id : null,
                best.Score >= 0.62 ? best.Product.Title : null,
                Math.Round(best.Score, 3),
                quantity > 0 ? quantityConfidence : 0.15,
                candidates,
                line));
        }

        return parsed
            .GroupBy(item => $"{item.MatchedProductId?.ToString() ?? NormalizeForMatch(item.RawName)}|{item.SourceLine}")
            .Select(group => group.First())
            .Take(200)
            .ToArray();
    }

    private static bool IsPotentialItemLine(string line)
    {
        var normalized = NormalizeForMatch(line);
        if (IgnoreLineTerms.Any(term => normalized.Contains(NormalizeForMatch(term), StringComparison.Ordinal)))
        {
            return false;
        }

        return line.Any(char.IsLetter) && NumberRegex().IsMatch(line);
    }

    private static double ScoreMatch(CatalogProduct product, string normalizedLine)
    {
        if (normalizedLine.Length == 0)
        {
            return 0;
        }

        if (normalizedLine.Contains(product.NormalizedTitle, StringComparison.Ordinal))
        {
            return 0.99;
        }

        var lineTokens = Tokenize(normalizedLine).ToHashSet(StringComparer.Ordinal);
        if (lineTokens.Count == 0)
        {
            return 0;
        }

        var matchedTokens = product.Tokens.Count(lineTokens.Contains);
        var coverage = matchedTokens / (double)product.Tokens.Length;
        var union = product.Tokens.Union(lineTokens, StringComparer.Ordinal).Count();
        var jaccard = union == 0 ? 0 : matchedTokens / (double)union;

        var importantTokens = product.Tokens.Where(token => token.Length >= 4).ToArray();
        var importantMatched = importantTokens.Length == 0
            ? coverage
            : importantTokens.Count(lineTokens.Contains) / (double)importantTokens.Length;

        return Math.Min(0.98, (coverage * 0.58) + (importantMatched * 0.30) + (jaccard * 0.12));
    }

    private static decimal ExtractQuantity(string line, string? productTitle, out double confidence)
    {
        confidence = 0;
        var working = line;

        if (!string.IsNullOrWhiteSpace(productTitle))
        {
            var directIndex = working.IndexOf(productTitle, StringComparison.OrdinalIgnoreCase);
            if (directIndex >= 0)
            {
                var suffix = working[(directIndex + productTitle.Length)..];
                var direct = FindFirstPositiveNumber(suffix);
                if (direct > 0)
                {
                    confidence = 0.92;
                    return direct;
                }
            }
        }

        var firstLetter = working.TakeWhile(character => !char.IsLetter(character)).Count();
        if (firstLetter > 0 && firstLetter < working.Length)
        {
            working = working[firstLetter..];
        }

        var productNumbers = string.IsNullOrWhiteSpace(productTitle)
            ? new HashSet<string>()
            : NumberRegex().Matches(productTitle)
                .Select(match => NormalizeNumberToken(match.Value))
                .ToHashSet(StringComparer.Ordinal);

        var matches = NumberRegex().Matches(working)
            .Where(match => !match.Value.Contains('%'))
            .Where(match => !productNumbers.Contains(NormalizeNumberToken(match.Value)))
            .ToArray();

        foreach (var match in matches)
        {
            if (!TryParseDecimal(match.Value, out var value) || value <= 0)
            {
                continue;
            }

            // Quantities normally appear before unit price and line total. Large decimal values are more likely prices.
            if (value <= 100000)
            {
                confidence = matches.Length >= 2 ? 0.68 : 0.52;
                return value;
            }
        }

        return 0;
    }

    private static decimal FindFirstPositiveNumber(string text)
    {
        foreach (Match match in NumberRegex().Matches(text))
        {
            if (match.Value.Contains('%'))
            {
                continue;
            }

            if (TryParseDecimal(match.Value, out var value) && value > 0 && value <= 100000)
            {
                return value;
            }
        }

        return 0;
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var cleaned = value.Trim().Replace(" ", string.Empty).Replace("%", string.Empty);

        if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            var lastComma = cleaned.LastIndexOf(',');
            var lastDot = cleaned.LastIndexOf('.');
            cleaned = lastComma > lastDot
                ? cleaned.Replace(".", string.Empty).Replace(',', '.')
                : cleaned.Replace(",", string.Empty);
        }
        else if (cleaned.Contains(','))
        {
            cleaned = cleaned.Replace(',', '.');
        }

        return decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
    }

    private static string ExtractRawDescription(string line)
    {
        var cleaned = Regex.Replace(line, @"^\s*\d{1,4}[\.)]?\s+", string.Empty);
        var firstPriceColumn = Regex.Match(cleaned, @"\s{1,}\d+(?:[.,]\d+)?(?:\s+\d+(?:[.,]\d+)?){1,}");
        if (firstPriceColumn.Success && firstPriceColumn.Index >= 3)
        {
            cleaned = cleaned[..firstPriceColumn.Index];
        }

        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim(' ', '-', '|', ':');
        return cleaned.Length > 180 ? cleaned[..180] : cleaned;
    }

    private async Task<bool> InvoiceAlreadyImportedAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM \"StockEntries\" WHERE LOWER(\"InvoiceNumber\") = LOWER(@invoiceNumber)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@invoiceNumber";
            parameter.Value = invoiceNumber.Trim();
            command.Parameters.Add(parameter);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(value, CultureInfo.InvariantCulture) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string? FindInvoiceNumber(string text)
    {
        var match = InvoiceNumberRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups[1].Value.Trim().Trim(':', '-', '#');
        return value.Length is >= 2 and <= 100 ? value : null;
    }

    private static string? FindInvoiceDate(string text)
    {
        var match = DateRegex().Match(text);
        return match.Success ? match.Value : null;
    }

    private static string DetectLanguage(string text)
    {
        var cyrillic = text.Count(character => character is >= '\u0400' and <= '\u04FF');
        var latin = text.Count(character => character is >= 'A' and <= 'z');
        return cyrillic > Math.Max(8, latin / 5) ? "bg" : "en";
    }

    private static string NormalizeExtractedText(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\u00A0', ' ')
            .Trim();
    }

    private static string NormalizeForMatch(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousSpace = false;

        foreach (var character in value.ToLowerInvariant())
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

    private static string[] Tokenize(string normalized)
    {
        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static int CountLetters(string value) => value.Count(char.IsLetter);

    private static string NormalizeNumberToken(string value) => value.Replace(" ", string.Empty).Replace(',', '.');

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[-+]?\d{1,6}(?:[\s.,]\d{3})*(?:[.,]\d+)?%?", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(?im)(?:invoice|фактура)\s*(?:no\.?|number|№|#)?\s*[:\-]?\s*([A-ZА-Я0-9][A-ZА-Я0-9\/\-.]{1,99})", RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberRegex();

    [GeneratedRegex(@"\b(?:0?[1-9]|[12]\d|3[01])[.\-/](?:0?[1-9]|1[0-2])[.\-/](?:20)?\d{2}\b", RegexOptions.CultureInvariant)]
    private static partial Regex DateRegex();
}
