using HygiaTrade.Common.Requests.Review;
using HygiaTrade.Common.Responses.Review;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public class ReviewService(IReviewRepository reviewRepository, IProductRepository productRepository, IAuthService authService, IUserRepository userRepository, IOrderRepository orderRepository) : IReviewService
{
    public async Task<ReviewResponse?> UpdateAsync(UpdateReviewRequest request)
    {
        Review? existingReview = await reviewRepository.GetByIdAsync(request.Id);
        if (existingReview == null) throw new AppException("Review not found.").SetStatusCode(404);
        if (existingReview.UserId != Guid.Parse(await authService.GetCurrentUserId())) throw new AppException("Can't edit a review that is not yours.").SetStatusCode(403);
        existingReview.Content = request.Content?.Trim() ?? string.Empty;
        existingReview.Rating = request.Rating;
        Review updatedReview = await reviewRepository.UpdateAsync(existingReview);
        await RecalculateProductRatingAsync(updatedReview!.ProductId);
        return new() { Id = updatedReview.Id, Content = updatedReview.Content, Rating = updatedReview.Rating, CreatedOn = updatedReview.CreatedOn, UserId = updatedReview.UserId, UserNames = (await userRepository.GetByIdAsync(updatedReview.UserId)).Names };
    }

    public async Task<ReviewResponse?> CreateAsync(CreateReviewRequest request)
    {
        Guid userId = Guid.Parse(await authService.GetCurrentUserId());
        if (!await orderRepository.HasConfirmedPurchaseAsync(userId, request.ProductId))
            throw new AppException("A confirmed purchase of this product is required before leaving a review.").SetStatusCode(403);

        IEnumerable<Review> productReviews = await reviewRepository.GetReviews(request.ProductId);
        if (productReviews.Any(x => x.UserId == userId))
            throw new AppException("You have already reviewed this product.").SetStatusCode(409);

        Review newReview = new()
        {
            ProductId = request.ProductId,
            UserId = userId,
            Content = request.Content?.Trim() ?? string.Empty,
            Rating = request.Rating,
        };
        await reviewRepository.AddAsync(newReview);
        await RecalculateProductRatingAsync(newReview.ProductId);
        return new() { Id = newReview.Id, Content = newReview.Content, Rating = newReview.Rating, CreatedOn = newReview.CreatedOn, UserId = newReview.UserId, UserNames = (await userRepository.GetByIdAsync(newReview.UserId)).Names };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Review? review = await reviewRepository.GetByIdAsync(id);
        if (review == null) throw new AppException("Review not found.").SetStatusCode(404);
        if (review.UserId != Guid.Parse(await authService.GetCurrentUserId())) throw new AppException("Can't delete a review that is not yours.").SetStatusCode(403);
        Guid productId = review.ProductId;
        bool deleted = await reviewRepository.DeleteAsync(id);
        if (deleted) await RecalculateProductRatingAsync(productId);
        return deleted;
    }

    public async Task<Paginated<ReviewResponse>> SearchReviewsAsync(SearchReviewsRequest request)
    {
        Filter<Review> filter = new()
        {
            Includes = [x => x.Product!], Predicate = request.GetPredicate(), PageNumber = request.PageNumber ?? 1, PageSize = request.PageSize ?? 10, SortBy = request.SortBy ?? "CreatedOn", SortDescending = request.SortDescending ?? false,
        };
        Paginated<Review> result = await reviewRepository.SearchAsync(filter);
        List<ReviewResponse> responses = new();
        foreach (Review review in result.Items!)
            responses.Add(new() { Id = review.Id, Content = review.Content, Rating = review.Rating, CreatedOn = review.CreatedOn, UserId = review.UserId, UserNames = (await userRepository.GetByIdAsync(review.UserId)).Names });
        return new() { Items = responses, TotalCount = result.TotalCount };
    }

    private async Task RecalculateProductRatingAsync(Guid productId)
    {
        Review[] reviewArray = (await reviewRepository.GetReviews(productId)).ToArray();
        double rating = reviewArray.Length == 0 ? 0 : Math.Round(reviewArray.Average(x => (double)x.Rating), 2, MidpointRounding.AwayFromZero);
        await productRepository.UpdateRatingAsync(productId, rating);
    }
}
