using HygiaTrade.API.Services;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class InvoiceParserTests
{
    private readonly InvoiceParser parser = new();

    [Fact]
    public void Parse_ExtractsMetadataLanguageQuantityAndBestMatch()
    {
        Guid productId = Guid.NewGuid();

        ParsedInvoice result = parser.Parse(
            """
            Invoice No: INV-123
            Date: 24.09.2026
            Widget 2 pcs 10 EUR
            """,
            new[]
            {
                new InvoiceCatalogProduct(productId, "Widget")
            });

        Assert.Equal("INV-123", result.InvoiceNumber);
        Assert.Equal("24.09.2026", result.InvoiceDate);
        Assert.Equal("en", result.DetectedLanguage);

        var item = Assert.Single(result.Items);
        Assert.Equal("Widget", item.RawName);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(productId, item.MatchedProductId);
        Assert.Equal("Widget", item.MatchedProductName);
        Assert.Equal(0.99, item.MatchConfidence);
        Assert.Equal(0.98, item.QuantityConfidence);
    }

    [Fact]
    public void Parse_DetectsBulgarianAndMarkerInvoiceNumber()
    {
        ParsedInvoice result = parser.Parse(
            """
            ФАКТУРА
            № BG-2026-77
            Дата: 25.09.2026
            1. Продукт Алфа 3 бр.
            """,
            Array.Empty<InvoiceCatalogProduct>());

        Assert.Equal("BG-2026-77", result.InvoiceNumber);
        Assert.Equal("25.09.2026", result.InvoiceDate);
        Assert.Equal("bg", result.DetectedLanguage);

        var item = Assert.Single(result.Items);
        Assert.Equal("Продукт Алфа", item.RawName);
        Assert.Equal(3m, item.Quantity);
        Assert.Null(item.MatchedProductId);
    }

    [Fact]
    public void Parse_UsesLooseInvoiceNumberFallback()
    {
        ParsedInvoice result = parser.Parse(
            """
            ACME-2026-991
            Widget 1 pcs
            """,
            Array.Empty<InvoiceCatalogProduct>());

        Assert.Equal("ACME-2026-991", result.InvoiceNumber);
    }

    [Fact]
    public void Parse_UsesUnlabeledDateFallback()
    {
        ParsedInvoice result = parser.Parse(
            """
            Reference 12345
            25/09/2026
            Widget 1 pcs
            """,
            Array.Empty<InvoiceCatalogProduct>());

        Assert.Equal("25/09/2026", result.InvoiceDate);
    }

    [Fact]
    public void Parse_RejectsTotalsDatesAndNonItemLines()
    {
        ParsedInvoice result = parser.Parse(
            """
            Invoice No: INV-1
            Date: 25.09.2026
            Total Widget 2 pcs
            25.09.2026
            12345
            Header without numbers
            """,
            new[]
            {
                new InvoiceCatalogProduct(Guid.NewGuid(), "Widget")
            });

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Parse_AcceptsNumberedItemWithoutCatalogMatch()
    {
        ParsedInvoice result = parser.Parse(
            "1. Unknown Product 4 10 EUR",
            Array.Empty<InvoiceCatalogProduct>());

        var item = Assert.Single(result.Items);
        Assert.Equal("Unknown Product 4", item.RawName);
        Assert.Equal(4m, item.Quantity);
        Assert.Equal(0.62, item.QuantityConfidence);
        Assert.Empty(item.Candidates);
    }

    [Fact]
    public void Parse_AcceptsQuantityUnitWithoutCatalogMatch()
    {
        ParsedInvoice result = parser.Parse(
            "Unknown Product 7 qty.",
            Array.Empty<InvoiceCatalogProduct>());

        var item = Assert.Single(result.Items);
        Assert.Equal(7m, item.Quantity);
        Assert.Equal(0.98, item.QuantityConfidence);
    }

    [Fact]
    public void Parse_PreservesExistingDecimalQuantityHeuristic()
    {
        ParsedInvoice result = parser.Parse(
            "1. Unknown Product 1.5 pcs",
            Array.Empty<InvoiceCatalogProduct>());

        var item = Assert.Single(result.Items);
        Assert.Equal(5m, item.Quantity);
        Assert.Equal("Unknown Product 1.", item.RawName);
    }

    [Fact]
    public void Parse_DeduplicatesSameProductName()
    {
        ParsedInvoice result = parser.Parse(
            """
            1. Widget 2 pcs
            2. Widget 3 pcs
            """,
            new[]
            {
                new InvoiceCatalogProduct(Guid.NewGuid(), "Widget")
            });

        var item = Assert.Single(result.Items);
        Assert.Equal(2m, item.Quantity);
    }

    [Fact]
    public void Parse_ProvidesCandidatesBelowAutoMatchThreshold()
    {
        Guid productId = Guid.NewGuid();

        ParsedInvoice result = parser.Parse(
            "1. Alpha Gamma 2 pcs",
            new[]
            {
                new InvoiceCatalogProduct(
                    productId,
                    "Alpha Beta Gamma Delta")
            });

        var item = Assert.Single(result.Items);
        Assert.NotEmpty(item.Candidates);
        Assert.Null(item.MatchedProductId);
        Assert.True(item.MatchConfidence >= 0.30);
        Assert.True(item.MatchConfidence < 0.58);
    }

    [Fact]
    public void Parse_IgnoresCatalogTitlesWithoutTokens()
    {
        ParsedInvoice result = parser.Parse(
            "1. Unknown Product 2 pcs",
            new[]
            {
                new InvoiceCatalogProduct(Guid.NewGuid(), "-")
            });

        var item = Assert.Single(result.Items);
        Assert.Empty(item.Candidates);
    }
}
