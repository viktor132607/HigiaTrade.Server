using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Responses.Gdpr;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class GdprControllerTests
{
    private readonly Mock<IGdprService> gdprService = new();

    private GdprController CreateController() =>
        new(gdprService.Object);

    [Fact]
    public async Task ExportData_ReturnsOk_WithServiceResponse()
    {
        GdprExportResponse expected = new()
        {
            RequestedAtUtc = DateTime.UtcNow,
            WishlistProductIds = [Guid.NewGuid()]
        };

        gdprService
            .Setup(service => service.ExportCurrentUserDataAsync())
            .ReturnsAsync(expected);

        ActionResult<GdprExportResponse> result =
            await CreateController().ExportData();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expected, ok.Value);

        gdprService.Verify(
            service => service.ExportCurrentUserDataAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ExportData_PropagatesException_FromService()
    {
        InvalidOperationException expected =
            new("Export failed.");

        gdprService
            .Setup(service => service.ExportCurrentUserDataAsync())
            .ThrowsAsync(expected);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateController().ExportData());

        Assert.Same(expected, actual);

        gdprService.Verify(
            service => service.ExportCurrentUserDataAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_ReturnsOk_WithServiceResponse()
    {
        GdprDeleteResponse expected = new()
        {
            Deleted = true,
            Message = "Account deleted."
        };

        gdprService
            .Setup(service => service.DeleteCurrentUserDataAsync())
            .ReturnsAsync(expected);

        ActionResult<GdprDeleteResponse> result =
            await CreateController().DeleteAccount();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expected, ok.Value);

        gdprService.Verify(
            service => service.DeleteCurrentUserDataAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_PropagatesException_FromService()
    {
        InvalidOperationException expected =
            new("Delete failed.");

        gdprService
            .Setup(service => service.DeleteCurrentUserDataAsync())
            .ThrowsAsync(expected);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateController().DeleteAccount());

        Assert.Same(expected, actual);

        gdprService.Verify(
            service => service.DeleteCurrentUserDataAsync(),
            Times.Once);
    }
}
