namespace HygiaTrade.API.Services;

public interface IDatabaseBackupFileStore
{
    string CreateTemporaryPath(string extension);

    Task CopyToFileAsync(
        Stream source,
        string destinationPath,
        CancellationToken cancellationToken);

    long GetLength(string filePath);

    void TryDelete(string filePath);
}

public sealed class DatabaseBackupFileStore
    : IDatabaseBackupFileStore
{
    private const int CopyBufferSize = 128 * 1024;

    public string CreateTemporaryPath(
        string extension)
    {
        string fileName =
            $"higiatrade-db-{Guid.NewGuid():N}.{extension}";

        return Path.Combine(
            Path.GetTempPath(),
            fileName);
    }

    public async Task CopyToFileAsync(
        Stream source,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using FileStream destination = new(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            CopyBufferSize,
            FileOptions.Asynchronous |
            FileOptions.SequentialScan);

        await source.CopyToAsync(
            destination,
            CopyBufferSize,
            cancellationToken);
    }

    public long GetLength(string filePath) =>
        new FileInfo(filePath).Length;

    public void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Temporary-file cleanup must never hide
            // the original backup/restore result.
        }
    }
}
