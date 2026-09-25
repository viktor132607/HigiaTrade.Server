using HygiaTrade.API.Services;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class StockEntryReportReaderTests
{
    [Fact]
    public async Task ReadSafeAsync_ReturnsEmpty_WhenUnderlyingProviderCannotRunRawSql()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"stock-report-{Guid.NewGuid():N}")
                .Options;

        await using var db =
            new ApplicationDbContext(options);

        var reader =
            new StockEntryReportReader(
                db,
                Mock.Of<ILogger<StockEntryReportReader>>());

        var result =
            await reader.ReadSafeAsync(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                CancellationToken.None);

        Assert.Empty(result);
    }
}
