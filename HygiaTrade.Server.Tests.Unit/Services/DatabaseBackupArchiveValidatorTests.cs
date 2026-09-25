using System.Text;
using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class DatabaseBackupArchiveValidatorTests
{
    private readonly DatabaseBackupArchiveValidator validator = new();

    [Fact]
    public async Task ValidateHeaderAsync_AcceptsPgDumpMagic()
    {
        string path = Path.GetTempFileName();

        try
        {
            await File.WriteAllBytesAsync(
                path,
                [
                    .. Encoding.ASCII.GetBytes("PGDMP"),
                    1,
                    2,
                    3
                ]);

            await validator.ValidateHeaderAsync(
                path,
                CancellationToken.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("PGDM")]
    [InlineData("WRONG")]
    public async Task ValidateHeaderAsync_RejectsInvalidHeader(
        string content)
    {
        string path = Path.GetTempFileName();

        try
        {
            await File.WriteAllBytesAsync(
                path,
                Encoding.ASCII.GetBytes(content));

            await Assert.ThrowsAsync<InvalidDataException>(
                () => validator.ValidateHeaderAsync(
                    path,
                    CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
