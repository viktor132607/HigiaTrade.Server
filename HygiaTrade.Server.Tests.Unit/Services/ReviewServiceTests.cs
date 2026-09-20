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
        private readonly Mock<IOrderRepository> orderRepositoryMock;
        private readonly ReviewService reviewService;

        public ReviewServiceTests()
        {
            reviewRepositoryMock = new();
            productRepositoryMock = new();
            authServiceMock = new();
            userRepositoryMock = new();
            orderRepositoryMock = new();
            reviewService = new(
                reviewRepositoryMock.Object,
                productRepositoryMock.Object,
                authServiceMock.Object,
                userRepositoryMock.Object,
                orderRepositoryMock.Object);
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
            Guid userId = Guid.NewGuid();
            Guid productId = Guid.NewGuid();
            UpdateReviewRequest request = new()
            {
                Id = Guid.NewGuid(),
                Content = "Updated Content",
                Rating = 5
            };

            Review existingReview = new()
            {
                Id = request.Id,
                UserId = userId,
                ProductId = productId,
                Content = "Old Content",
                Rating = 3
            };
            authServiceMock.Setup(a => a.GetCurrentUserId()).ReturnsAsync(userId.ToString());
            reviewRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingReview);
            reviewRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Review>())).ReturnsAsync(existingReview);
            reviewRepositoryMock.Setup(r => r.GetReviews(productId)).ReturnsAsync(new[] { existingReview });
            productRepositoryMock.Setup(p => p.UpdateRatingAsync(productId, It.IsAny<double>())).Returns(Task.CompletedTask);
            userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User
            {
                Names = "John Doe",
                Email = "john@example.com",
                Phone = "123"
            });

            ReviewResponse result = await reviewService.UpdateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Updated Content", result.Content);
            Assert.Equal(5, result.Rating);
        }

        [Fact]
        public async Task CreateAsync_ValidReview_ShouldReturnCreatedReview()
        {
            Guid userId = Guid.NewGuid();
            CreateReviewRequest request = new()
            {
                ProductId = Guid.NewGuid(),
                Content = "Great Product",
                Rating = 5
            };

            authServiceMock.Setup(a => a.GetCurrentUserId()).ReturnsAsync(userId.ToString());
            orderRepositoryMock.Setup(r => r.HasConfirmedPurchaseAsync(userId, request.ProductId)).ReturnsAsync(true);
            reviewRepositoryMock.Setup(r => r.GetReviews(request.ProductId)).ReturnsAsync(Array.Empty<Review>());
            reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Review>())).ReturnsAsync((Review review) => review);
            productRepositoryMock.Setup(p => p.UpdateRatingAsync(request.ProductId, It.IsAny<double>())).Returns(Task.CompletedTask);
            userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User
            {
                Names = "Jane Doe",
                Email = "jane@example.com",
                Phone = "123"
            });

            ReviewResponse result = await reviewService.CreateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("Great Product", result.Content);
            Assert.Equal(5, result.Rating);
        }

        [Fact]
        public async Task CreateAsync_Review_ShouldRecalculateProductRating()
        {
            Guid userId = Guid.NewGuid();
            CreateReviewRequest request = new()
            {
                ProductId = Guid.NewGuid(),
                Content = "Excellent Product",
                Rating = 4
            };

            authServiceMock.Setup(a => a.GetCurrentUserId()).ReturnsAsync(userId.ToString());
            orderRepositoryMock.Setup(r => r.HasConfirmedPurchaseAsync(userId, request.ProductId)).ReturnsAsync(true);
            reviewRepositoryMock.Setup(r => r.GetReviews(request.ProductId)).ReturnsAsync(Array.Empty<Review>());
            reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Review>())).ReturnsAsync((Review review) => review);
            userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User
            {
                Names = "John Smith",
                Email = "john@example.com",
                Phone = "123"
            });
            productRepositoryMock.Setup(p => p.UpdateRatingAsync(It.IsAny<Guid>(), It.IsAny<double>())).Returns(Task.CompletedTask);

            await reviewService.CreateAsync(request);

            productRepositoryMock.Verify(p => p.UpdateRatingAsync(request.ProductId, It.IsAny<double>()), Times.Once);
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
