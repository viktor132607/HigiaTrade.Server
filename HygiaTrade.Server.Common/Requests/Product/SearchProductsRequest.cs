using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Data.PaginationAndFiltering;

namespace HygiaTrade.Common.Requests.Product;

public class SearchProductsRequest : PaginationModel
{ 
    public string? Title { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public byte? MinRating { get; set; }

    // Set by the API controller for authenticated admins; public callers cannot force inactive products to appear.
    public bool IncludeInactive { get; set; }

    public Expression<Func<Data.Entities.Product, bool>> GetPredicate()
    {
        Expression<Func<Data.Entities.Product, bool>> result = s => !s.IsDeleted;

        if (!IncludeInactive)
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, FilterByTitle());
        }

        if (CategoryId.HasValue)
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, FilterByCategory());
        }

        if (MinPrice.HasValue)
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, FilterByMinPrice());
        }

        if (MaxPrice.HasValue)
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, FilterByMaxPrice());
        }

        if (MinRating.HasValue)
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, FilterByMinRating());
        }

        return result;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByTitle()
    {
        return x => EF.Functions.Like(x.Title.ToLower(), $"%{Title!.ToLower()}%");
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByCategory()
    {
        return x => x.CategoryId == CategoryId!.Value;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByMinPrice()
    {
        return x => (x.DiscountedPrice > 0m ? x.DiscountedPrice : x.RegularPrice) >= MinPrice!.Value;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByMaxPrice()
    {
        return x => (x.DiscountedPrice > 0m ? x.DiscountedPrice : x.RegularPrice) <= MaxPrice!.Value;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByMinRating()
    {
        return x => x.Rating >= MinRating!.Value;
    }
}
