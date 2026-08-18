using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;

namespace HygiaTrade.Data.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity>
   where TEntity : class
    {
        private readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        public virtual async ValueTask<TEntity?> AddAsync(TEntity entity)
        {
            if (entity == null)
            {
                return null;
            }
            
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            IQueryable<TEntity> query = _context.Set<TEntity>().AsQueryable();
            PropertyInfo? isDeletedProperty = typeof(TEntity).GetProperty("IsDeleted");

            if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
            {
                query = query.Where(e => EF.Property<bool>(e, "IsDeleted") == false);
            }

            return await query.ToListAsync();
        }

        public virtual async ValueTask<TEntity?> GetByIdAsync(Guid id)
        {
            TEntity? entity = await _context.Set<TEntity>().FindAsync(id);
            PropertyInfo? isDeletedProperty = typeof(TEntity).GetProperty("IsDeleted");

            if (entity != null && isDeletedProperty != null)
            {
                bool isDeleted = (bool)isDeletedProperty.GetValue(entity);
                if (isDeleted)
                {
                    return null;
                }
            }

            return entity;
        }

        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            TEntity? entity = await _context.Set<TEntity>().FindAsync(id);
            
            if (entity == null)
            {
                return false;
            }
            
            PropertyInfo? propertyInfo = entity.GetType().GetProperty("IsDeleted");

            if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
            {
                propertyInfo.SetValue(entity, true, null);
                _context.Set<TEntity>().Update(entity);
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                _context.Set<TEntity>().Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public virtual async ValueTask<TEntity?> UpdateAsync(TEntity entity)
        {
            if (entity == null)
            {
                return null;
            }
            
            Guid entityId = (Guid)entity.GetType().GetProperty("Id").GetValue(entity);
            
            TEntity? currentEntity = await _context.Set<TEntity>().FindAsync(entityId);

            if (currentEntity == null)
            {
                return null;
            }

            PropertyInfo? propertyInfo = entity.GetType().GetProperty("ModifiedOn");

            if (propertyInfo != null && propertyInfo.PropertyType == typeof(DateTime))
            {
                propertyInfo.SetValue(entity, DateTime.UtcNow, null);
            }

            EntityEntry<TEntity>? entry = _context.Entry(currentEntity);
            entry.CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<Paginated<TEntity>> SearchAsync(Filter<TEntity> request)
        {
            IQueryable<TEntity>? query = _context.Set<TEntity>().AsQueryable().AsNoTracking();

            foreach (Expression<Func<TEntity, object>> include in request.Includes)
            {
                query = query.Include(include);
            }

            foreach (string include in request.IncludesAsPropertyPath)
            {
                query = query.Include(include);
            }

            if (request.SortBy != null)
            {
                if (request.SortDescending == true)
                {
                    query = query.OrderBy($"{request.SortBy} DESC");
                }
                else
                {
                    query = query.OrderBy(request.SortBy);
                }
            }

            int count = query.Count(request.Predicate);

            if (request.PageNumber != null)
            {   
                int page = (int)request.PageNumber!;
                int itemsPerPage = (int)request.PageSize!;
                int skip = (page - 1) * itemsPerPage;
                List<TEntity> filteredItems = await query.Where(request.Predicate).Skip(skip).Take(itemsPerPage).ToListAsync().ConfigureAwait(false);

                return new()
                {
                    TotalCount = count,
                    Items = filteredItems,
                };
            }

            List<TEntity> items = await query.Where(request.Predicate).ToListAsync().ConfigureAwait(false);

            return new()
            {
                TotalCount = count,
                Items = items,
            };
        }
    }
}
