using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Pricing;
using HygiaTrade.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public sealed record ProductExcelImportRow(
    int RowNumber,
    string Title,
    string Description,
    string? Brand,
    string Category,
    string RegularPrice,
    string DiscountPercentage,
    string DiscountedPrice,
    string WholesalePrice,
    string WholesaleMinQuantity,
    string VatRate,
    string Quantity,
    string MainImageUrl,
    string IsActive);

public sealed record ProductExcelImportRowResult(
    int RowNumber,
    string Title,
    string Status,
    string Message,
    Guid? ProductId);

public sealed record ProductExcelImportResult(
    int TotalRows,
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<ProductExcelImportRowResult> Rows);

public sealed class ProductExcelImportException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

internal sealed class ProductExcelRowException(
    string message) : Exception(message);

public interface IProductExcelWorkbookReader
{
    IReadOnlyList<ProductExcelImportRow> Read(Stream stream);
}

public interface IProductExcelImportService
{
    Task<ProductExcelImportResult> ImportAsync(
        IFormFile? file,
        bool updateExisting,
        CancellationToken cancellationToken);
}

public sealed class ProductExcelWorkbookReader
    : IProductExcelWorkbookReader
{
    private const int MaxRows = 1000;

    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static readonly XNamespace DocumentRelationshipsNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly XNamespace PackageRelationshipsNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    private static readonly Dictionary<string, string> HeaderAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = "Title",
            ["name"] = "Title",
            ["product"] = "Title",
            ["productname"] = "Title",
            ["име"] = "Title",
            ["наименование"] = "Title",
            ["продукт"] = "Title",

            ["description"] = "Description",
            ["описание"] = "Description",

            ["brand"] = "Brand",
            ["марка"] = "Brand",
            ["бранд"] = "Brand",

            ["category"] = "Category",
            ["categoryid"] = "Category",
            ["категория"] = "Category",
            ["категорияid"] = "Category",

            ["regularprice"] = "RegularPrice",
            ["retailprice"] = "RegularPrice",
            ["price"] = "RegularPrice",
            ["цена"] = "RegularPrice",
            ["редовнацена"] = "RegularPrice",
            ["ценанадребно"] = "RegularPrice",

            ["discountpercentage"] = "DiscountPercentage",
            ["discountpercent"] = "DiscountPercentage",
            ["discount"] = "DiscountPercentage",
            ["отстъпка"] = "DiscountPercentage",
            ["отстъпкапроцент"] = "DiscountPercentage",

            ["discountedprice"] = "DiscountedPrice",
            ["promoprice"] = "DiscountedPrice",
            ["saleprice"] = "DiscountedPrice",
            ["промоционалнацена"] = "DiscountedPrice",
            ["промоцена"] = "DiscountedPrice",

            ["wholesaleprice"] = "WholesalePrice",
            ["ценанаедро"] = "WholesalePrice",

            ["wholesaleminquantity"] = "WholesaleMinQuantity",
            ["wholesaleminimumquantity"] = "WholesaleMinQuantity",
            ["минколичествоедро"] = "WholesaleMinQuantity",
            ["минималноколичествоедро"] = "WholesaleMinQuantity",

            ["vatrate"] = "VatRate",
            ["vat"] = "VatRate",
            ["ддс"] = "VatRate",
            ["ддспроцент"] = "VatRate",

            ["quantity"] = "Quantity",
            ["stock"] = "Quantity",
            ["наличност"] = "Quantity",
            ["количество"] = "Quantity",

            ["mainimageurl"] = "MainImageUrl",
            ["imageurl"] = "MainImageUrl",
            ["image"] = "MainImageUrl",
            ["снимка"] = "MainImageUrl",
            ["изображение"] = "MainImageUrl",

            ["isactive"] = "IsActive",
            ["active"] = "IsActive",
            ["активен"] = "IsActive",
            ["активно"] = "IsActive"
        };

    public IReadOnlyList<ProductExcelImportRow> Read(Stream stream)
    {
        try
        {
            using var archive =
                new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            XDocument workbook =
                LoadDocument(
                    archive,
                    "xl/workbook.xml",
                    "The Excel workbook is missing xl/workbook.xml.");

            XDocument relationships =
                LoadDocument(
                    archive,
                    "xl/_rels/workbook.xml.rels",
                    "The Excel workbook relationships are missing.");

            XElement? firstSheet = workbook
                .Descendants(SpreadsheetNamespace + "sheet")
                .FirstOrDefault();

            string? relationId =
                firstSheet?.Attribute(
                    DocumentRelationshipsNamespace + "id")?.Value;

            if (string.IsNullOrWhiteSpace(relationId))
            {
                throw InvalidWorkbook(
                    "The Excel workbook does not contain a readable worksheet.");
            }

            XElement? relation = relationships
                .Descendants(PackageRelationshipsNamespace + "Relationship")
                .FirstOrDefault(element =>
                    string.Equals(
                        element.Attribute("Id")?.Value,
                        relationId,
                        StringComparison.Ordinal));

            string? target = relation?.Attribute("Target")?.Value;

            if (string.IsNullOrWhiteSpace(target))
            {
                throw InvalidWorkbook(
                    "The first Excel worksheet could not be resolved.");
            }

            string worksheetPath =
                ResolveTarget("xl/workbook.xml", target);

            XDocument worksheet =
                LoadDocument(
                    archive,
                    worksheetPath,
                    "The first Excel worksheet is missing.");

            IReadOnlyList<string> sharedStrings =
                ReadSharedStrings(archive);

            List<(int RowNumber, Dictionary<int, string> Cells)> rows =
                worksheet
                    .Descendants(SpreadsheetNamespace + "row")
                    .Select((row, index) =>
                    {
                        int rowNumber =
                            int.TryParse(
                                row.Attribute("r")?.Value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int parsedRow)
                                ? parsedRow
                                : index + 1;

                        return (
                            rowNumber,
                            ReadCells(row, sharedStrings));
                    })
                    .Where(row =>
                        row.Item2.Values.Any(value =>
                            !string.IsNullOrWhiteSpace(value)))
                    .ToList();

            if (rows.Count == 0)
            {
                throw InvalidWorkbook(
                    "The Excel worksheet is empty.");
            }

            Dictionary<int, string> headers =
                BuildHeaderMap(rows[0].Cells);

            List<ProductExcelImportRow> result = [];

            foreach (var row in rows.Skip(1))
            {
                if (result.Count >= MaxRows)
                {
                    throw InvalidWorkbook(
                        $"A single Excel import can contain up to {MaxRows} product rows.");
                }

                ProductExcelImportRow item =
                    MapRow(row.RowNumber, row.Cells, headers);

                if (IsEmpty(item))
                {
                    continue;
                }

                result.Add(item);
            }

            return result;
        }
        catch (ProductExcelImportException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw InvalidWorkbook(
                "The uploaded file is not a valid .xlsx workbook.");
        }
        catch (Exception exception) when (
            exception is XmlException or
            IOException)
        {
            throw InvalidWorkbook(
                "The uploaded Excel workbook is malformed or unreadable.");
        }
    }

    private static XDocument LoadDocument(
        ZipArchive archive,
        string path,
        string errorMessage)
    {
        ZipArchiveEntry? entry = archive.GetEntry(path);

        if (entry is null)
        {
            throw InvalidWorkbook(errorMessage);
        }

        using Stream entryStream = entry.Open();

        return XDocument.Load(entryStream);
    }

    private static IReadOnlyList<string> ReadSharedStrings(
        ZipArchive archive)
    {
        ZipArchiveEntry? entry =
            archive.GetEntry("xl/sharedStrings.xml");

        if (entry is null)
        {
            return [];
        }

        using Stream stream = entry.Open();
        XDocument document = XDocument.Load(stream);

        return document
            .Descendants(SpreadsheetNamespace + "si")
            .Select(item => string.Concat(
                item.Descendants(SpreadsheetNamespace + "t")
                    .Select(text => text.Value)))
            .ToList();
    }

    private static Dictionary<int, string> ReadCells(
        XElement row,
        IReadOnlyList<string> sharedStrings)
    {
        Dictionary<int, string> cells = [];
        int fallbackColumn = 1;

        foreach (XElement cell in
                 row.Elements(SpreadsheetNamespace + "c"))
        {
            string? reference = cell.Attribute("r")?.Value;
            int column =
                GetColumnIndex(reference) ??
                fallbackColumn;

            fallbackColumn = column + 1;

            cells[column] =
                GetCellValue(cell, sharedStrings).Trim();
        }

        return cells;
    }

    private static string GetCellValue(
        XElement cell,
        IReadOnlyList<string> sharedStrings)
    {
        string? type = cell.Attribute("t")?.Value;

        if (string.Equals(
                type,
                "inlineStr",
                StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(
                cell.Descendants(SpreadsheetNamespace + "t")
                    .Select(text => text.Value));
        }

        string value =
            cell.Element(SpreadsheetNamespace + "v")?.Value ??
            string.Empty;

        if (string.Equals(
                type,
                "s",
                StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int sharedIndex) &&
            sharedIndex >= 0 &&
            sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        if (string.Equals(
                type,
                "b",
                StringComparison.OrdinalIgnoreCase))
        {
            return value == "1" ? "true" : "false";
        }

        return value;
    }

    private static int? GetColumnIndex(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        int index = 0;
        bool foundLetter = false;

        foreach (char character in reference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            foundLetter = true;
            index =
                index * 26 +
                (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return foundLetter ? index : null;
    }

    private static Dictionary<int, string> BuildHeaderMap(
        IReadOnlyDictionary<int, string> cells)
    {
        Dictionary<int, string> headers = [];
        HashSet<string> used =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in cells)
        {
            string normalized =
                NormalizeHeader(pair.Value);

            if (!HeaderAliases.TryGetValue(
                    normalized,
                    out string? canonical))
            {
                continue;
            }

            if (!used.Add(canonical))
            {
                throw InvalidWorkbook(
                    $"The Excel worksheet contains the '{canonical}' column more than once.");
            }

            headers[pair.Key] = canonical;
        }

        string[] required =
            ["Title", "Category", "RegularPrice"];

        string[] missing =
            required
                .Where(requiredHeader =>
                    !headers.Values.Contains(
                        requiredHeader,
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();

        if (missing.Length > 0)
        {
            throw InvalidWorkbook(
                $"Missing required Excel columns: {string.Join(", ", missing)}.");
        }

        return headers;
    }

    private static ProductExcelImportRow MapRow(
        int rowNumber,
        IReadOnlyDictionary<int, string> cells,
        IReadOnlyDictionary<int, string> headers)
    {
        Dictionary<string, string> values =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            cells.TryGetValue(
                header.Key,
                out string? value);

            values[header.Value] =
                value?.Trim() ?? string.Empty;
        }

        string Get(string key) =>
            values.TryGetValue(key, out string? value)
                ? value
                : string.Empty;

        string brand = Get("Brand");

        return new ProductExcelImportRow(
            rowNumber,
            Get("Title"),
            Get("Description"),
            string.IsNullOrWhiteSpace(brand)
                ? null
                : brand,
            Get("Category"),
            Get("RegularPrice"),
            Get("DiscountPercentage"),
            Get("DiscountedPrice"),
            Get("WholesalePrice"),
            Get("WholesaleMinQuantity"),
            Get("VatRate"),
            Get("Quantity"),
            Get("MainImageUrl"),
            Get("IsActive"));
    }

    private static bool IsEmpty(
        ProductExcelImportRow row) =>
        string.IsNullOrWhiteSpace(row.Title) &&
        string.IsNullOrWhiteSpace(row.Category) &&
        string.IsNullOrWhiteSpace(row.RegularPrice) &&
        string.IsNullOrWhiteSpace(row.Description) &&
        string.IsNullOrWhiteSpace(row.Brand);

    private static string NormalizeHeader(string value)
    {
        return new string(
            value
                .Trim()
                .ToLowerInvariant()
                .Where(character =>
                    char.IsLetterOrDigit(character))
                .ToArray());
    }

    private static string ResolveTarget(
        string basePath,
        string target)
    {
        if (target.StartsWith('/'))
        {
            return target.TrimStart('/');
        }

        string baseDirectory =
            basePath.Contains('/')
                ? basePath[..basePath.LastIndexOf('/')]
                : string.Empty;

        List<string> parts =
            $"{baseDirectory}/{target}"
                .Replace('\\', '/')
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries)
                .ToList();

        Stack<string> resolved = new();

        foreach (string part in parts)
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (resolved.Count > 0)
                {
                    resolved.Pop();
                }

                continue;
            }

            resolved.Push(part);
        }

        return string.Join(
            "/",
            resolved.Reverse());
    }

    private static ProductExcelImportException InvalidWorkbook(
        string message) =>
        new(StatusCodes.Status400BadRequest, message);
}

public sealed class ProductExcelImportService(
    ApplicationDbContext db,
    IProductExcelWorkbookReader workbookReader,
    IProductPricingPolicy pricingPolicy)
    : IProductExcelImportService
{
    private const long MaxFileSize =
        10L * 1024L * 1024L;

    public async Task<ProductExcelImportResult> ImportAsync(
        IFormFile? file,
        bool updateExisting,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw BadRequest(
                "Choose a non-empty .xlsx file.");
        }

        if (!file.FileName.EndsWith(
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            throw BadRequest(
                "Only .xlsx Excel files are supported.");
        }

        if (file.Length > MaxFileSize)
        {
            throw BadRequest(
                "The Excel file cannot exceed 10 MB.");
        }

        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        IReadOnlyList<ProductExcelImportRow> rows =
            workbookReader.Read(memory);

        if (rows.Count == 0)
        {
            return new ProductExcelImportResult(
                0,
                0,
                0,
                0,
                0,
                []);
        }

        List<Category> categories =
            await db.Categories
                .AsNoTracking()
                .Where(category => !category.IsDeleted)
                .ToListAsync(cancellationToken);

        Dictionary<Guid, Category> categoriesById =
            categories.ToDictionary(
                category => category.Id);

        Dictionary<string, Category> categoriesByName =
            categories
                .GroupBy(
                    category => category.Name.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

        List<Product> existingProducts =
            await db.Products
                .Where(product => !product.IsDeleted)
                .ToListAsync(cancellationToken);

        Dictionary<string, List<Product>> productsByKey =
            existingProducts
                .GroupBy(BuildProductKey)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.OrdinalIgnoreCase);

        HashSet<string> workbookKeys =
            new(StringComparer.OrdinalIgnoreCase);

        List<ProductExcelImportRowResult> results = [];
        int created = 0;
        int updated = 0;
        int skipped = 0;

        foreach (ProductExcelImportRow row in rows)
        {
            try
            {
                ParsedProductRow parsed =
                    ParseRow(row);

                string productKey =
                    BuildProductKey(
                        parsed.Title,
                        parsed.Brand);

                if (!workbookKeys.Add(productKey))
                {
                    throw new ProductExcelRowException(
                        "Duplicate product row in the same Excel file.");
                }

                Category category =
                    ResolveCategory(
                        parsed.Category,
                        categoriesById,
                        categoriesByName);

                pricingPolicy.Validate(
                    parsed.RegularPrice,
                    parsed.DiscountPercentage,
                    parsed.DiscountedPrice,
                    parsed.WholesalePrice,
                    parsed.WholesaleMinQuantity,
                    parsed.VatRate);

                productsByKey.TryGetValue(
                    productKey,
                    out List<Product>? matches);

                if (matches is { Count: > 1 })
                {
                    throw new ProductExcelRowException(
                        "More than one existing product matches this title and brand.");
                }

                if (matches is { Count: 1 })
                {
                    Product existing = matches[0];

                    if (!updateExisting)
                    {
                        skipped++;
                        results.Add(
                            new ProductExcelImportRowResult(
                                row.RowNumber,
                                parsed.Title,
                                "skipped",
                                "Product already exists. Enable update existing products to overwrite it.",
                                existing.Id));

                        continue;
                    }

                    Apply(
                        existing,
                        parsed,
                        category.Id);

                    updated++;
                    results.Add(
                        new ProductExcelImportRowResult(
                            row.RowNumber,
                            parsed.Title,
                            "updated",
                            "Existing product updated.",
                            existing.Id));

                    continue;
                }

                var product = new Product
                {
                    Title = parsed.Title,
                    Brand = parsed.Brand,
                    Description = parsed.Description,
                    MainImageUrl = parsed.MainImageUrl,
                    IsActive = parsed.IsActive,
                    Rating = 0,
                    Quantity = parsed.Quantity,
                    CategoryId = category.Id,
                    WholesalePrice =
                        ProductPricingCalculator.RoundMoney(
                            parsed.WholesalePrice),
                    WholesaleMinQuantity =
                        parsed.WholesaleMinQuantity,
                    VatRate = parsed.VatRate
                };

                pricingPolicy.ApplyRetailPricing(
                    product,
                    parsed.RegularPrice,
                    parsed.DiscountPercentage,
                    parsed.DiscountedPrice);

                db.Products.Add(product);

                productsByKey[productKey] = [product];

                created++;
                results.Add(
                    new ProductExcelImportRowResult(
                        row.RowNumber,
                        parsed.Title,
                        "created",
                        "Product created.",
                        product.Id));
            }
            catch (ProductExcelRowException exception)
            {
                results.Add(
                    new ProductExcelImportRowResult(
                        row.RowNumber,
                        row.Title.Trim(),
                        "error",
                        exception.Message,
                        null));
            }
            catch (AppException exception)
            {
                results.Add(
                    new ProductExcelImportRowResult(
                        row.RowNumber,
                        row.Title.Trim(),
                        "error",
                        exception.Message,
                        null));
            }
        }

        if (created > 0 || updated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        int failed =
            results.Count(result =>
                result.Status == "error");

        return new ProductExcelImportResult(
            rows.Count,
            created,
            updated,
            skipped,
            failed,
            results);
    }

    private void Apply(
        Product product,
        ParsedProductRow parsed,
        Guid categoryId)
    {
        product.Title = parsed.Title;
        product.Brand = parsed.Brand;
        product.Description = parsed.Description;
        product.MainImageUrl = parsed.MainImageUrl;
        product.IsActive = parsed.IsActive;
        product.CategoryId = categoryId;
        product.Quantity = parsed.Quantity;
        product.WholesalePrice =
            ProductPricingCalculator.RoundMoney(
                parsed.WholesalePrice);
        product.WholesaleMinQuantity =
            parsed.WholesaleMinQuantity;
        product.VatRate = parsed.VatRate;
        product.ModifiedOn = DateTime.UtcNow;

        pricingPolicy.ApplyRetailPricing(
            product,
            parsed.RegularPrice,
            parsed.DiscountPercentage,
            parsed.DiscountedPrice);
    }

    private static ParsedProductRow ParseRow(
        ProductExcelImportRow row)
    {
        string title = row.Title.Trim();

        if (title.Length == 0)
        {
            throw new ProductExcelRowException(
                "Title is required.");
        }

        if (title.Length > 300)
        {
            throw new ProductExcelRowException(
                "Title cannot exceed 300 characters.");
        }

        string category = row.Category.Trim();

        if (category.Length == 0)
        {
            throw new ProductExcelRowException(
                "Category is required.");
        }

        string? brand =
            string.IsNullOrWhiteSpace(row.Brand)
                ? null
                : row.Brand.Trim();

        decimal regularPrice =
            ParseRequiredDecimal(
                row.RegularPrice,
                "RegularPrice");

        byte discountPercentage =
            ParsePercentage(
                row.DiscountPercentage,
                "DiscountPercentage");

        decimal discountedPrice =
            ParseOptionalDecimal(
                row.DiscountedPrice,
                "DiscountedPrice",
                0m);

        decimal wholesalePrice =
            ParseOptionalDecimal(
                row.WholesalePrice,
                "WholesalePrice",
                0m);

        uint wholesaleMinQuantity =
            ParseOptionalUInt(
                row.WholesaleMinQuantity,
                "WholesaleMinQuantity",
                0);

        decimal vatRate =
            ParseOptionalDecimal(
                row.VatRate,
                "VatRate",
                20m);

        uint quantity =
            ParseOptionalUInt(
                row.Quantity,
                "Quantity",
                0);

        bool isActive =
            ParseOptionalBoolean(
                row.IsActive,
                "IsActive",
                true);

        return new ParsedProductRow(
            title,
            row.Description.Trim(),
            brand,
            category,
            regularPrice,
            discountPercentage,
            discountedPrice,
            wholesalePrice,
            wholesaleMinQuantity,
            vatRate,
            quantity,
            row.MainImageUrl.Trim(),
            isActive);
    }

    private static Category ResolveCategory(
        string value,
        IReadOnlyDictionary<Guid, Category> categoriesById,
        IReadOnlyDictionary<string, Category> categoriesByName)
    {
        if (Guid.TryParse(
                value,
                out Guid categoryId) &&
            categoriesById.TryGetValue(
                categoryId,
                out Category? categoryById))
        {
            return categoryById;
        }

        if (categoriesByName.TryGetValue(
                value.Trim(),
                out Category? categoryByName))
        {
            return categoryByName;
        }

        throw new ProductExcelRowException(
            $"Category '{value}' does not exist.");
    }

    private static decimal ParseRequiredDecimal(
        string value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ProductExcelRowException(
                $"{field} is required.");
        }

        return ParseDecimal(value, field);
    }

    private static decimal ParseOptionalDecimal(
        string value,
        string field,
        decimal defaultValue) =>
        string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : ParseDecimal(value, field);

    private static decimal ParseDecimal(
        string value,
        string field)
    {
        string normalized = value.Trim();

        if (normalized.Contains(',') &&
            !normalized.Contains('.'))
        {
            normalized =
                normalized.Replace(',', '.');
        }

        if (decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal parsed))
        {
            return parsed;
        }

        throw new ProductExcelRowException(
            $"{field} must be a valid number.");
    }

    private static byte ParsePercentage(
        string value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (int.TryParse(
                value.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed) &&
            parsed >= 0 &&
            parsed <= 100)
        {
            return (byte)parsed;
        }

        throw new ProductExcelRowException(
            $"{field} must be a whole number between 0 and 100.");
    }

    private static uint ParseOptionalUInt(
        string value,
        string field,
        uint defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (uint.TryParse(
                value.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out uint parsed))
        {
            return parsed;
        }

        throw new ProductExcelRowException(
            $"{field} must be a non-negative whole number.");
    }

    private static bool ParseOptionalBoolean(
        string value,
        string field,
        bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "yes" or "y" or
            "да" or "активен" or "активно" => true,

            "false" or "0" or "no" or "n" or
            "не" or "неактивен" or "неактивно" => false,

            _ => throw new ProductExcelRowException(
                $"{field} must be true/false, yes/no, 1/0 or да/не.")
        };
    }

    private static string BuildProductKey(Product product) =>
        BuildProductKey(product.Title, product.Brand);

    private static string BuildProductKey(
        string title,
        string? brand) =>
        $"{title.Trim()}\u001f{brand?.Trim() ?? string.Empty}";

    private static ProductExcelImportException BadRequest(
        string message) =>
        new(StatusCodes.Status400BadRequest, message);

    private sealed record ParsedProductRow(
        string Title,
        string Description,
        string? Brand,
        string Category,
        decimal RegularPrice,
        byte DiscountPercentage,
        decimal DiscountedPrice,
        decimal WholesalePrice,
        uint WholesaleMinQuantity,
        decimal VatRate,
        uint Quantity,
        string MainImageUrl,
        bool IsActive);
}
