using Moq;
using HygiaTrade.Common.Requests.Review;
using HygiaTrade.Common.Responses.Review;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Domain.Interfaces;
using Xunit;

namespace HygiaTrade.Tests.Unit.Services
{
    public class ReviewServiceTests
    {
        private readonly Mock<IReviewRepository> reviewRepositoryMock;
        private readonly Mock<IProductRepository> productRepositoryMock;
        private readonly Mock<IAuthService> authServiceMock;
        private readonly Mock<IUserRepository> userRepositoryMock;
        private readonly ReviewService reviewService;

        public ReviewServiceTests()
        {
            reviewRepositoryMock = new();
            productRepositoryMock = new();
            authServiceMock = new();
            userRepositoryMock = new();
            reviewService = new(
                reviewRepositoryMock.Object,
                productRepositoryMock.Object,
                authServiceMock.Object,
                userRepositoryMock.Object);
        }

        [Fact]
        public async Task UpdateAsync_ReviewNotFound_ShouldThrowNotFound()
        {
            UpdateReviewRequest request = new()
            {
                Id = Guid.NewGuid(),
                Content = null,
                Rating = 0
            };
            reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Review)null);

            await Assert.ThrowsAsync<AppException>(() => reviewService.UpdateAsync(request));
        }

        [Fact]
        public async Task UpdateAsync_ValidReview_ShouldReturnUpdatedReview()
        {
            UpdateReviewRequest request = new()
            {
                Id = Guid.NewGuid(),
                Content = "Updated Content",
                Rating = 5
            };

            Review existingReview = new() { Id = request.Id, Content = "Old Content", Rating = 3 };
            reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingReview);
            reviewRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Review>())).ReturnsAsync(existingReview);
            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User
            {
                Names = "John Doe",
                Email = null,
                Phone = null
            });

            ReviewResponse result = await reviewService.UpdateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Updated Content", result.Content);
            Assert.Equal(5, result.Rating);
        }

        [Fact]
        public async Task CreateAsync_ValidReview_ShouldReturnCreatedReview()
        {
            CreateReviewRequest request = new()
            {
                ProductId = Guid.NewGuid(),
                Content = "Great Product",
                Rating = 5
            };

            authServiceMock.Setup(a => a.GetCurrentUserId()).ReturnsAsync(Guid.NewGuid().ToString());
            reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Review>())).ReturnsAsync(new Review { Id = Guid.NewGuid() });
            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User
            {
                Names = "Jane Doe",
                Email = null,
                Phone = null
            });

            ReviewResponse result = await reviewService.CreateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Great Product", result.Content);
            Assert.Equal(5, result.Rating);
        }

        [Fact]
        public async Task CreateAsync_Review_ShouldRecalculateProductRating()
        {
            CreateReviewRequest request = new()
            {
                ProductId = Guid.NewGuid(),
                Content = "Excellent Product",
                Rating = 4
            };

            authServiceMock.Setup(a => a.GetCurrentUserId()).ReturnsAsync(Guid.NewGuid().ToString());
            reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Review>())).ReturnsAsync(new Review { Id = Guid.NewGuid() });
            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User
            {
                Names = "John Smith",
                Email = null,
                Phone = null
            });
            productRepositoryMock.Setup(p => p.UpdateRatingAsync(It.IsAny<Guid>(), It.IsAny<double>())).Returns(Task.CompletedTask);

            await reviewService.CreateAsync(request);

            productRepositoryMock.Verify(p => p.UpdateRatingAsync(It.IsAny<Guid>(), It.IsAny<double>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ReviewNotFound_ShouldThrowNotFound()
        {
            reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Review)null);

            await Assert.ThrowsAsync<AppException>(() => reviewService.DeleteAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteAsync_UserNotOwner_ShouldThrowForbidden()
        {
            Guid reviewId = Guid.NewGuid();
            Review review = new() { Id = reviewId, UserId = Guid.NewGuid() };
            reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(review);
            authServiceMock.Setup(a => a.GetCurrentUserId()).ReturnsAsync(Guid.NewGuid().ToString());

            await Assert.ThrowsAsync<AppException>(() => reviewService.DeleteAsync(reviewId));
        }

        [Fact]
        public async Task DeleteAsync_ValidReview_ShouldDeleteReview()
        {
            Guid reviewId = Guid.NewGuid();
            Review review = new() { Id = reviewId, UserId = Guid.NewGuid() };
            reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(review);
            authServiceMock.Setup(a => a.GetCurrentUserId()).ReturnsAsync(review.UserId.ToString());
            reviewRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            bool result = await reviewService.DeleteAsync(reviewId);

            Assert.True(result);
        }
    }
}
