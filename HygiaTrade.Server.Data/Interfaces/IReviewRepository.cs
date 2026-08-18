using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<IEnumerable<Review>> GetReviews(Guid productId);
}
