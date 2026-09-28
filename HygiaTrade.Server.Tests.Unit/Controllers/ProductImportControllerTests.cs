using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class ProductImportControllerTests
{
    [Fact]
    public async Task ImportExcelAsync_ReturnsOk_WithImportSummary()
    {
        var service = new Mock<IProductExcelImportService>();
        IFormFile file = File();
        var expected = new ProductExcelImportResult(
            2,
            1,
            1,
            0,
            0,
            []);

        service
            .Setup(x => x.ImportAsync(
                file,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller =
            new ProductImportController(service.Object);

        IActionResult action =
            await controller.ImportExcelAsync(
                file,
                true,
                CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(action);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task ImportExcelAsync_MapsImportExceptionToStatusCode()
    {
        var service = new Mock<IProductExcelImportService>();
        IFormFile file = File();

        service
            .Setup(x => x.ImportAsync(
                file,
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new ProductExcelImportException(
                    StatusCodes.Status400BadRequest,
                    "Bad workbook"));

        var controller =
            new ProductImportController(service.Object);

        ObjectResult result =
            Assert.IsType<ObjectResult>(
                await controller.ImportExcelAsync(
                    file,
                    true,
                    CancellationToken.None));

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            result.StatusCode);
    }

    private static IFormFile File()
    {
        var stream =
            new MemoryStream([1, 2, 3]);

        return new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "products.xlsx");
    }
}
