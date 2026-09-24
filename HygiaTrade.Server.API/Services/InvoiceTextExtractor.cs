using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace HygiaTrade.API.Services;

public sealed record InvoiceTextExtraction(
    string FileName,
    string Text);

public sealed record InvoiceProcessResult(
    int ExitCode,
    string StdOut,
    string StdErr);

public interface IInvoiceTextExtractor
{
    Task<InvoiceTextExtraction> ExtractAsync(
        IFormFile? file,
        CancellationToken cancellationToken);
}

public interface IInvoiceProcessRunner
{
    Task<InvoiceProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public sealed class InvoiceTextExtractor(
    IInvoiceProcessRunner processRunner) : IInvoiceTextExtractor
{
    private const long MaxFileSize = 15 * 1024 * 1024;
    private const int MaxPdfPages = 20;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".png", ".jpg", ".jpeg", ".webp"
        };

    public async Task<InvoiceTextExtraction> ExtractAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "Choose an invoice file.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "The invoice file cannot exceed 15 MB.");
        }

        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "Supported formats are PDF, PNG, JPG, JPEG and WEBP.");
        }

        string tempRoot = Path.Combine(
            Path.GetTempPath(),
            "hygiatrade-invoices",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempRoot);

        string inputPath = Path.Combine(
            tempRoot,
            $"invoice{extension.ToLowerInvariant()}");

        try
        {
            await using (FileStream stream = File.Create(inputPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            string extractedText = extension.Equals(
                ".pdf",
                StringComparison.OrdinalIgnoreCase)
                ? await ExtractPdfTextAsync(
                    inputPath,
                    tempRoot,
                    cancellationToken)
                : await OcrImageAsync(
                    inputPath,
                    cancellationToken);

            extractedText = NormalizeExtractedText(extractedText);

            if (CountLetters(extractedText) < 8)
            {
                throw new InvoiceImportServiceException(
                    StatusCodes.Status422UnprocessableEntity,
                    "No readable invoice text was detected. Try a sharper image or a higher-quality PDF.");
            }

            return new InvoiceTextExtraction(
                file.FileName,
                extractedText);
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
                // Best-effort temp cleanup only.
            }
        }
    }

    private async Task<string> ExtractPdfTextAsync(
        string inputPath,
        string tempRoot,
        CancellationToken cancellationToken)
    {
        InvoiceProcessResult digitalText = await processRunner.RunAsync(
            "pdftotext",
            ["-layout", "-enc", "UTF-8", inputPath, "-"],
            TimeSpan.FromSeconds(20),
            cancellationToken);

        if (digitalText.ExitCode == 0 &&
            CountLetters(digitalText.StdOut) >= 40)
        {
            return digitalText.StdOut;
        }

        string pagePrefix = Path.Combine(tempRoot, "page");
        InvoiceProcessResult render = await processRunner.RunAsync(
            "pdftoppm",
            [
                "-png",
                "-r",
                "150",
                "-f",
                "1",
                "-l",
                MaxPdfPages.ToString(CultureInfo.InvariantCulture),
                inputPath,
                pagePrefix
            ],
            TimeSpan.FromSeconds(45),
            cancellationToken);

        if (render.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"The PDF could not be rendered for OCR: {render.StdErr.Trim()}");
        }

        string[] pageFiles = Directory
            .GetFiles(tempRoot, "page-*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (pageFiles.Length == 0)
        {
            throw new InvalidOperationException(
                "The PDF did not contain readable pages.");
        }

        var builder = new StringBuilder();
        foreach (string pageFile in pageFiles)
        {
            builder.AppendLine(
                await OcrImageAsync(
                    pageFile,
                    cancellationToken));
        }

        return builder.ToString();
    }

    private async Task<string> OcrImageAsync(
        string inputPath,
        CancellationToken cancellationToken)
    {
        InvoiceProcessResult result = await processRunner.RunAsync(
            "tesseract",
            [
                inputPath,
                "stdout",
                "-l",
                "bul+eng",
                "--oem",
                "1",
                "--psm",
                "6"
            ],
            TimeSpan.FromSeconds(60),
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"OCR failed: {result.StdErr.Trim()}");
        }

        return result.StdOut;
    }

    private static string NormalizeExtractedText(string text) => text
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n')
        .Replace('\u00A0', ' ')
        .Trim();

    private static int CountLetters(string value) =>
        value.Count(char.IsLetter);
}

public sealed class InvoiceProcessRunner : IInvoiceProcessRunner
{
    public static async Task<InvoiceProcessResult> RunProcessAsync(
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

        startInfo.Environment["OMP_THREAD_LIMIT"] = "1";
        startInfo.Environment["OMP_NUM_THREADS"] = "1";

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)
                ?? throw new FileNotFoundException(
                    $"Required OCR executable '{executable}' is not available on the server.");
        }
        catch (Win32Exception)
        {
            throw new FileNotFoundException(
                $"Required OCR executable '{executable}' is not available on the server.");
        }

        using (process)
        using (var timeoutCts =
               CancellationTokenSource.CreateLinkedTokenSource(
                   cancellationToken))
        {
            timeoutCts.CancelAfter(timeout);

            Task<string> stdoutTask =
                process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            Task<string> stderrTask =
                process.StandardError.ReadToEndAsync(timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // Best effort only.
                }

                throw new TimeoutException(
                    $"Invoice OCR did not finish within {timeout.TotalSeconds:0} seconds.");
            }

            return new InvoiceProcessResult(
                process.ExitCode,
                await stdoutTask,
                await stderrTask);
        }
    }

    public Task<InvoiceProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        RunProcessAsync(
            executable,
            arguments,
            timeout,
            cancellationToken);
}
