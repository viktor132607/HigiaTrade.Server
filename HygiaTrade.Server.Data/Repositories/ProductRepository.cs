using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;

namespace HygiaTrade.Data.Repositories;

public class ProductRepository(ApplicationDbContext context) : Repository<Product>(context), IProductRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Paginated<Product>> SearchAsync(Filter<Product> filter)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SecondaryImages);

        foreach (Expression<Func<Product, object>> include in filter.Includes)
        {
            query = query.Include(include);
        }

        foreach (string include in filter.IncludesAsPropertyPath)
        {
            query = query.Include(include);
        }

        if (filter.SortBy != null)
        {
            query = filter.SortDescending == true
                ? query.OrderBy($"{filter.SortBy} DESC")
                : query.OrderBy(filter.SortBy);
        }

        int count = query.Count(filter.Predicate);

        if (filter.PageNumber != null)
        {
            int page = (int)filter.PageNumber!;
            int itemsPerPage = (int)filter.PageSize!;
            int skip = (page - 1) * itemsPerPage;
            List<Product> filteredItems = await query
                .Where(filter.Predicate)
                .Skip(skip)
                .Take(itemsPerPage)
                .ToListAsync()
                .ConfigureAwait(false);

            return new()
            {
                TotalCount = count,
                Items = filteredItems,
            };
        }

        List<Product> items = await query
            .Where(filter.Predicate)
            .ToListAsync()
            .ConfigureAwait(false);

        return new()
        {
            TotalCount = count,
            Items = items,
        };
    }
    
    public override async ValueTask<Product?> GetByIdAsync(Guid id)
    {
        Product? product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.SecondaryImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        PropertyInfo? isDeletedProperty = typeof(Product).GetProperty("IsDeleted");

        if (product != null && isDeletedProperty != null)
        {
            bool isDeleted = (bool)isDeletedProperty.GetValue(product);
            if (isDeleted)
            {
                return null;
            }
        }

        return product;
    }
    
    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        IQueryable<Product> query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.SecondaryImages);

        PropertyInfo? isDeletedProperty = typeof(Product).GetProperty("IsDeleted");

        if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
        {
            query = query.Where(p => EF.Property<bool>(p, "IsDeleted") == false);
        }

        return await query.ToListAsync();
    }
    public async Task<IEnumerable<Product>> GetBestSellersAsync(int numOfBestSellers)
    {
        IQueryable<Product> query = _context.Set<Product>()
            .Where(product => product.IsActive)
            .AsQueryable();
        PropertyInfo? isDeletedProperty = typeof(Product).GetProperty("IsDeleted");

        if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
        {
            query = query.Where(e => EF.Property<bool>(e, "IsDeleted") == false);
        }

        return await query
            .Include(x => x.Category)
            .Include(x => x.SecondaryImages)
            .OrderBy(x => Guid.NewGuid())
            .Take(numOfBestSellers)
            .ToListAsync();
    }

    public async Task UpdateRatingAsync(Guid productId, double rating)
    {
        Product product = await GetByIdAsync(productId);

        product.Rating = rating;

        await UpdateAsync(product);
    }
}
