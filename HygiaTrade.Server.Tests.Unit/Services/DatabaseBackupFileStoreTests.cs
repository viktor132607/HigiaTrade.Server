using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class DatabaseBackupFileStoreTests
{
    private readonly DatabaseBackupFileStore store = new();

    [Fact]
    public void CreateTemporaryPath_UsesExpectedPrefixAndExtension()
    {
        string path =
            store.CreateTemporaryPath("dump");

        Assert.Equal(
            Path.GetTempPath(),
            Path.GetDirectoryName(path) +
            Path.DirectorySeparatorChar);

        Assert.StartsWith(
            "higiatrade-db-",
            Path.GetFileName(path));

        Assert.EndsWith(".dump", path);
    }

    [Fact]
    public async Task CopyToFileAsync_CopiesContentAndGetLengthReturnsSize()
    {
        string path =
            store.CreateTemporaryPath("dump");

        try
        {
            byte[] data = [1, 2, 3, 4, 5];

            await store.CopyToFileAsync(
                new MemoryStream(data),
                path,
                CancellationToken.None);

            Assert.Equal(data.Length, store.GetLength(path));
            Assert.Equal(data, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            store.TryDelete(path);
        }
    }

    [Fact]
    public void TryDelete_RemovesExistingFileAndIgnoresMissingFile()
    {
        string path = Path.GetTempFileName();

        store.TryDelete(path);
        store.TryDelete(path);

        Assert.False(File.Exists(path));
    }
}
