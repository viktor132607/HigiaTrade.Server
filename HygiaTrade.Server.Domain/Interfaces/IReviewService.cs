using HygiaTrade.Common.Requests.Review;
using HygiaTrade.Common.Responses.Review;
using HygiaTrade.Core.Pages;

namespace HygiaTrade.Domain.Interfaces;

public interface IReviewService
{
    Task<ReviewResponse?> UpdateAsync(UpdateReviewRequest request);
    Task<ReviewResponse?> CreateAsync(CreateReviewRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<Paginated<ReviewResponse>> SearchReviewsAsync(SearchReviewsRequest request);
}
