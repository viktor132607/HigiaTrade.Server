using System.Linq.Expressions;
using HygiaTrade.Data.PaginationAndFiltering;

namespace HygiaTrade.Common.Requests.Order;

public class SearchOrderRequest : PaginationModel
{
    public Guid? UserId { get; set; }

    public Guid? OrderId { get; set; }
    
    public Expression<Func<Data.Entities.Order, bool>> GetPredicate()
    {
        Expression<Func<Data.Entities.Order, bool>> result = s => !s.IsDeleted;

        if (UserId.HasValue)
        {
            result = ExpressionExtension<Data.Entities.Order>.AndAlso(result, FilterByUserId());
        }

        if (OrderId.HasValue)
        {
            result = ExpressionExtension<Data.Entities.Order>.AndAlso(result, FilterByOrderId());
        }
        
        return result;
    }

    private Expression<Func<Data.Entities.Order, bool>> FilterByUserId()
    {
        return x => x.UserId == UserId;
    }

    private Expression<Func<Data.Entities.Order, bool>> FilterByOrderId()
    {
        return x => x.Id == OrderId;
    }
}
