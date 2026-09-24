using HygiaTrade.API.Controllers;
using HygiaTrade.API.Models;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class ReportsControllerTests
{
    private readonly Mock<IReportsService> reportsService = new();
    private readonly Mock<ILogger<ReportsController>> logger = new();

    private ReportsController CreateController() =>
        new(reportsService.Object, logger.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk_WithReport()
    {
        DateOnly from = new(2026, 1, 1);
        DateOnly to = new(2026, 1, 31);
        AdminReportResponse expected = CreateReport(from, to, 5);

        reportsService
            .Setup(service => service.GetAsync(
                from,
                to,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAsync(
                from,
                to,
                5,
                CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetAsync_ReturnsMappedStatus_WhenReportsServiceExceptionOccurs()
    {
        ReportsServiceException exception =
            new(400, "Invalid date range.");

        reportsService
            .Setup(service => service.GetAsync(
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().GetAsync(
                cancellationToken: CancellationToken.None);

        ObjectResult error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, error.StatusCode);
        Assert.Equal(
            exception.Message,
            error.Value?.GetType().GetProperty("message")?.GetValue(error.Value));
    }

    [Fact]
    public async Task GetAsync_Returns500_WhenUnexpectedExceptionOccurs()
    {
        reportsService
            .Setup(service => service.GetAsync(
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB failed."));

        IActionResult result =
            await CreateController().GetAsync(
                cancellationToken: CancellationToken.None);

        ObjectResult error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            error.StatusCode);

        Assert.Equal(
            "Справката не можа да бъде заредена. Провери логовете на сървъра.",
            error.Value?.GetType().GetProperty("message")?.GetValue(error.Value));
    }

    private static AdminReportResponse CreateReport(
        DateOnly from,
        DateOnly to,
        int threshold) =>
        new(
            from,
            to,
            threshold,
            new ReportSummary(0, 0, 0, 0, 0, 0, 0, 0m),
            [],
            [],
            [],
            []);
}
