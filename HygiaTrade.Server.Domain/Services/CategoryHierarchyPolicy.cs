using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface ICategoryHierarchyPolicy
{
    Task<Guid?> ValidateParentAsync(
        Guid? parentCategoryId,
        Guid? currentCategoryId);

    void EnsureCanDelete(
        Guid categoryId,
        IEnumerable<Category> categories);
}

public sealed class CategoryHierarchyPolicy(
    ICategoryRepository categoryRepository)
    : ICategoryHierarchyPolicy
{
    public async Task<Guid?> ValidateParentAsync(
        Guid? parentCategoryId,
        Guid? currentCategoryId)
    {
        if (!parentCategoryId.HasValue)
        {
            return null;
        }

        if (currentCategoryId.HasValue &&
            parentCategoryId == currentCategoryId)
        {
            throw new AppException(
                    "A category cannot be its own parent.")
                .SetStatusCode(400);
        }

        Category? parent =
            await categoryRepository.GetByIdAsync(
                parentCategoryId.Value);

        if (parent is null || parent.IsDeleted)
        {
            throw new AppException(
                    "Parent category not found.")
                .SetStatusCode(404);
        }

        if (parent.ParentCategoryId.HasValue)
        {
            throw new AppException(
                    "Only one subcategory level is supported.")
                .SetStatusCode(400);
        }

        return parent.Id;
    }

    public void EnsureCanDelete(
        Guid categoryId,
        IEnumerable<Category> categories)
    {
        bool hasSubcategories =
            categories.Any(category =>
                !category.IsDeleted &&
                category.ParentCategoryId ==
                    categoryId);

        if (hasSubcategories)
        {
            throw new AppException(
                    "Delete or move the subcategories first.")
                .SetStatusCode(409);
        }
    }
}
