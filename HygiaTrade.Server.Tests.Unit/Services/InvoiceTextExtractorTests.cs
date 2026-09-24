using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class InvoiceTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_RejectsNullFile()
    {
        var extractor =
            new InvoiceTextExtractor(new FakeProcessRunner());

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => extractor.ExtractAsync(
                    null,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task ExtractAsync_RejectsEmptyFile()
    {
        var extractor =
            new InvoiceTextExtractor(new FakeProcessRunner());

        IFormFile file = CreateFile(
            Array.Empty<byte>(),
            "invoice.png");

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => extractor.ExtractAsync(
                    file,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task ExtractAsync_RejectsOversizedFile()
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(item => item.Length)
            .Returns((15L * 1024 * 1024) + 1);
        file.SetupGet(item => item.FileName)
            .Returns("invoice.pdf");

        var extractor =
            new InvoiceTextExtractor(new FakeProcessRunner());

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => extractor.ExtractAsync(
                    file.Object,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("15 MB", exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_RejectsUnsupportedExtension()
    {
        var extractor =
            new InvoiceTextExtractor(new FakeProcessRunner());

        IFormFile file = CreateFile(
            "content"u8.ToArray(),
            "invoice.txt");

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => extractor.ExtractAsync(
                    file,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("Supported formats", exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_OcrsImageAndNormalizesText()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue((executable, _) =>
        {
            Assert.Equal("tesseract", executable);
            return new InvoiceProcessResult(
                0,
                "Invoice\r\nWidget\u00A0quantity 2\r",
                string.Empty);
        });

        var extractor = new InvoiceTextExtractor(runner);

        InvoiceTextExtraction result =
            await extractor.ExtractAsync(
                CreateFile(
                    "fake-image"u8.ToArray(),
                    "invoice.PNG"),
                CancellationToken.None);

        Assert.Equal("invoice.PNG", result.FileName);
        Assert.Equal(
            "Invoice\nWidget quantity 2",
            result.Text);
    }

    [Fact]
    public async Task ExtractAsync_RejectsUnreadableText()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue((_, _) =>
            new InvoiceProcessResult(
                0,
                "123 456",
                string.Empty));

        var extractor = new InvoiceTextExtractor(runner);

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => extractor.ExtractAsync(
                    CreateFile(
                        "fake-image"u8.ToArray(),
                        "invoice.jpg"),
                    CancellationToken.None));

        Assert.Equal(
            StatusCodes.Status422UnprocessableEntity,
            exception.StatusCode);
    }

    [Fact]
    public async Task ExtractAsync_ThrowsWhenOcrFails()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue((_, _) =>
            new InvoiceProcessResult(
                1,
                string.Empty,
                "ocr error"));

        var extractor = new InvoiceTextExtractor(runner);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => extractor.ExtractAsync(
                    CreateFile(
                        "fake-image"u8.ToArray(),
                        "invoice.webp"),
                    CancellationToken.None));

        Assert.Contains("OCR failed: ocr error", exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_UsesDigitalPdfTextWhenReadable()
    {
        string digitalText =
            "This is readable invoice text with more than forty alphabetic characters for extraction.";

        var runner = new FakeProcessRunner();
        runner.Enqueue((executable, _) =>
        {
            Assert.Equal("pdftotext", executable);
            return new InvoiceProcessResult(
                0,
                digitalText,
                string.Empty);
        });

        var extractor = new InvoiceTextExtractor(runner);

        InvoiceTextExtraction result =
            await extractor.ExtractAsync(
                CreateFile(
                    "fake-pdf"u8.ToArray(),
                    "invoice.pdf"),
                CancellationToken.None);

        Assert.Equal(digitalText, result.Text);
        Assert.Equal(1, runner.Calls);
    }

    [Fact]
    public async Task ExtractAsync_ThrowsWhenPdfRenderingFails()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue((_, _) =>
            new InvoiceProcessResult(
                1,
                string.Empty,
                "no digital text"));

        runner.Enqueue((executable, _) =>
        {
            Assert.Equal("pdftoppm", executable);
            return new InvoiceProcessResult(
                2,
                string.Empty,
                "render failed");
        });

        var extractor = new InvoiceTextExtractor(runner);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => extractor.ExtractAsync(
                    CreateFile(
                        "fake-pdf"u8.ToArray(),
                        "invoice.pdf"),
                    CancellationToken.None));

        Assert.Contains(
            "render failed",
            exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_ThrowsWhenRenderedPdfHasNoPages()
    {
        var runner = new FakeProcessRunner();
        runner.Enqueue((_, _) =>
            new InvoiceProcessResult(
                1,
                string.Empty,
                string.Empty));

        runner.Enqueue((_, _) =>
            new InvoiceProcessResult(
                0,
                string.Empty,
                string.Empty));

        var extractor = new InvoiceTextExtractor(runner);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => extractor.ExtractAsync(
                    CreateFile(
                        "fake-pdf"u8.ToArray(),
                        "invoice.pdf"),
                    CancellationToken.None));

        Assert.Contains(
            "did not contain readable pages",
            exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_FallsBackToOcrForRenderedPdfPages()
    {
        var runner = new FakeProcessRunner();

        runner.Enqueue((_, _) =>
            new InvoiceProcessResult(
                1,
                string.Empty,
                string.Empty));

        runner.Enqueue((executable, arguments) =>
        {
            Assert.Equal("pdftoppm", executable);

            string pagePrefix = arguments[^1];
            File.WriteAllText(
                $"{pagePrefix}-1.png",
                "fake");

            return new InvoiceProcessResult(
                0,
                string.Empty,
                string.Empty);
        });

        runner.Enqueue((executable, _) =>
        {
            Assert.Equal("tesseract", executable);
            return new InvoiceProcessResult(
                0,
                "Readable invoice page text",
                string.Empty);
        });

        var extractor = new InvoiceTextExtractor(runner);

        InvoiceTextExtraction result =
            await extractor.ExtractAsync(
                CreateFile(
                    "fake-pdf"u8.ToArray(),
                    "invoice.pdf"),
                CancellationToken.None);

        Assert.Contains(
            "Readable invoice page text",
            result.Text);
        Assert.Equal(3, runner.Calls);
    }

    [Fact]
    public async Task ProcessRunner_ThrowsFileNotFound_ForMissingExecutable()
    {
        var runner = new InvoiceProcessRunner();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => runner.RunAsync(
                $"missing-invoice-tool-{Guid.NewGuid():N}",
                Array.Empty<string>(),
                TimeSpan.FromSeconds(2),
                CancellationToken.None));
    }

    private static IFormFile CreateFile(
        byte[] bytes,
        string fileName)
    {
        var stream = new MemoryStream(bytes);

        return new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            fileName);
    }

    private sealed class FakeProcessRunner : IInvoiceProcessRunner
    {
        private readonly Queue<
            Func<
                string,
                IReadOnlyList<string>,
                InvoiceProcessResult>> handlers = new();

        public int Calls { get; private set; }

        public void Enqueue(
            Func<
                string,
                IReadOnlyList<string>,
                InvoiceProcessResult> handler) =>
            handlers.Enqueue(handler);

        public Task<InvoiceProcessResult> RunAsync(
            string executable,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            Calls++;

            if (handlers.Count == 0)
            {
                throw new InvalidOperationException(
                    "No fake process result configured.");
            }

            return Task.FromResult(
                handlers.Dequeue()(executable, arguments));
        }
    }
}
