using System.IO.Compression;
using System.Security;
using System.Text;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ProductExcelWorkbookReaderTests
{
    [Fact]
    public void Read_ParsesEnglishHeadersAndValues()
    {
        byte[] workbook = BuildWorkbook(
            ["Title", "Category", "RegularPrice", "Brand", "Quantity", "IsActive"],
            ["Widget", "Tools", "12.50", "Acme", "7", "true"]);

        using var stream = new MemoryStream(workbook);

        IReadOnlyList<ProductExcelImportRow> rows =
            new ProductExcelWorkbookReader().Read(stream);

        ProductExcelImportRow row = Assert.Single(rows);
        Assert.Equal(2, row.RowNumber);
        Assert.Equal("Widget", row.Title);
        Assert.Equal("Tools", row.Category);
        Assert.Equal("12.50", row.RegularPrice);
        Assert.Equal("Acme", row.Brand);
        Assert.Equal("7", row.Quantity);
        Assert.Equal("true", row.IsActive);
    }

    [Fact]
    public void Read_RecognizesBulgarianHeaderAliases()
    {
        byte[] workbook = BuildWorkbook(
            ["Име", "Категория", "Цена", "Марка", "Наличност", "ДДС"],
            ["Продукт", "Хигиена", "8,40", "Марка", "5", "20"]);

        using var stream = new MemoryStream(workbook);

        ProductExcelImportRow row =
            Assert.Single(
                new ProductExcelWorkbookReader().Read(stream));

        Assert.Equal("Продукт", row.Title);
        Assert.Equal("Хигиена", row.Category);
        Assert.Equal("8,40", row.RegularPrice);
        Assert.Equal("Марка", row.Brand);
        Assert.Equal("5", row.Quantity);
        Assert.Equal("20", row.VatRate);
    }

    [Fact]
    public void Read_Throws400_WhenRequiredHeaderIsMissing()
    {
        byte[] workbook = BuildWorkbook(
            ["Title", "RegularPrice"],
            ["Widget", "10"]);

        using var stream = new MemoryStream(workbook);

        ProductExcelImportException exception =
            Assert.Throws<ProductExcelImportException>(
                () => new ProductExcelWorkbookReader().Read(stream));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("Category", exception.Message);
    }

    [Fact]
    public void Read_Throws400_WhenFileIsNotValidXlsx()
    {
        using var stream =
            new MemoryStream(Encoding.UTF8.GetBytes("not-a-zip"));

        ProductExcelImportException exception =
            Assert.Throws<ProductExcelImportException>(
                () => new ProductExcelWorkbookReader().Read(stream));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains(".xlsx", exception.Message);
    }

    [Fact]
    public void Read_SkipsCompletelyEmptyRows()
    {
        byte[] workbook = BuildWorkbook(
            ["Title", "Category", "RegularPrice"],
            ["", "", ""],
            ["Widget", "Tools", "10"]);

        using var stream = new MemoryStream(workbook);

        ProductExcelImportRow row =
            Assert.Single(
                new ProductExcelWorkbookReader().Read(stream));

        Assert.Equal(3, row.RowNumber);
        Assert.Equal("Widget", row.Title);
    }

    private static byte[] BuildWorkbook(params string[][] rows)
    {
        using var output = new MemoryStream();

        using (var archive =
               new ZipArchive(
                   output,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            AddEntry(
                archive,
                "xl/workbook.xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Products" sheetId="1" r:id="rId1" />
                  </sheets>
                </workbook>
                """);

            AddEntry(
                archive,
                "xl/_rels/workbook.xml.rels",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1"
                    Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"
                    Target="worksheets/sheet1.xml" />
                </Relationships>
                """);

            string rowXml =
                string.Join(
                    string.Empty,
                    rows.Select((row, rowIndex) =>
                        $"<row r=\"{rowIndex + 1}\">" +
                        string.Join(
                            string.Empty,
                            row.Select((value, columnIndex) =>
                                $"<c r=\"{ColumnName(columnIndex + 1)}{rowIndex + 1}\" t=\"inlineStr\"><is><t>{Escape(value)}</t></is></c>")) +
                        "</row>"));

            AddEntry(
                archive,
                "xl/worksheets/sheet1.xml",
                $"""
                 <?xml version="1.0" encoding="UTF-8"?>
                 <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                   <sheetData>{rowXml}</sheetData>
                 </worksheet>
                 """);
        }

        return output.ToArray();
    }

    private static void AddEntry(
        ZipArchive archive,
        string path,
        string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path);

        using Stream stream = entry.Open();
        using var writer =
            new StreamWriter(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        writer.Write(content.Trim());
    }

    private static string ColumnName(int index)
    {
        StringBuilder result = new();

        while (index > 0)
        {
            index--;
            result.Insert(0, (char)('A' + index % 26));
            index /= 26;
        }

        return result.ToString();
    }

    private static string Escape(string value) =>
        SecurityElement.Escape(value) ?? string.Empty;
}
