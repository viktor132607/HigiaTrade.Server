using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Review;
using HygiaTrade.Common.Responses.Review;
using HygiaTrade.Core.Pages;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class ReviewsControllerTests
{
    private readonly Mock<IReviewService> reviewService = new();

    private ReviewsController CreateController() =>
        new(reviewService.Object);

    [Fact]
    public async Task SearchAsync_ReturnsOk()
    {
        SearchReviewsRequest request = new()
        {
            ProductId = Guid.NewGuid()
        };
        Paginated<ReviewResponse> expected = new()
        {
            Items = [CreateReview()],
            TotalCount = 1
        };

        reviewService
            .Setup(service => service.SearchReviewsAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().SearchAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk()
    {
        CreateReviewRequest request = new()
        {
            ProductId = Guid.NewGuid(),
            Rating = 5,
            Content = "Great"
        };
        ReviewResponse expected = CreateReview();

        reviewService
            .Setup(service => service.CreateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().CreateAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk()
    {
        UpdateReviewRequest request = new()
        {
            Id = Guid.NewGuid(),
            Rating = 4,
            Content = "Updated"
        };
        ReviewResponse expected = CreateReview();

        reviewService
            .Setup(service => service.UpdateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().UpdateAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenDeleted()
    {
        Guid id = Guid.NewGuid();

        reviewService
            .Setup(service => service.DeleteAsync(id))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().DeleteAsync(id);

        Assert.IsType<OkResult>(result);
    }

    private static ReviewResponse CreateReview() => new()
    {
        Id = Guid.NewGuid(),
        Content = "Review",
        Rating = 5,
        UserNames = "User",
        CreatedOn = DateTime.UtcNow
    };
}
