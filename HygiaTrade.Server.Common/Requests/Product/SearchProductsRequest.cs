using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Data.PaginationAndFiltering;

namespace HygiaTrade.Common.Requests.Product;

public class SearchProductsRequest : PaginationModel
{
    public string? Title { get; set; }
    public string? Brand { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }

    public byte? MinRating { get; set; }
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

        if (!string.IsNullOrWhiteSpace(Brand))
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, FilterByBrand());
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
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, FilterByRatingBucket());
        }

        if (InStockOnly)
        {
            result = ExpressionExtension<Data.Entities.Product>.AndAlso(result, x => x.Quantity > 0);
        }

        return result;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByTitle()
    {
        return x => EF.Functions.Like(x.Title.ToLower(), $"%{Title!.ToLower()}%");
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByBrand()
    {
        var normalizedBrand = Brand!.Trim().ToLower();
        return x => x.Brand != null && x.Brand.ToLower() == normalizedBrand;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByCategory()
    {
        Guid categoryId = CategoryId!.Value;
        return x => x.CategoryId == categoryId || (x.Category != null && x.Category.ParentCategoryId == categoryId);
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByMinPrice()
    {
        return x => (x.DiscountedPrice > 0m ? x.DiscountedPrice : x.RegularPrice) >= MinPrice!.Value;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByMaxPrice()
    {
        return x => (x.DiscountedPrice > 0m ? x.DiscountedPrice : x.RegularPrice) <= MaxPrice!.Value;
    }

    private Expression<Func<Data.Entities.Product, bool>> FilterByRatingBucket()
    {
        return MinRating!.Value switch
        {
            1 => x => x.Rating >= 1.00 && x.Rating < 1.50,
            2 => x => x.Rating >= 1.50 && x.Rating < 2.50,
            3 => x => x.Rating >= 2.50 && x.Rating < 3.50,
            4 => x => x.Rating >= 3.50 && x.Rating < 4.50,
            5 => x => x.Rating >= 4.50 && x.Rating <= 5.00,
            _ => x => false,
        };
    }
}
