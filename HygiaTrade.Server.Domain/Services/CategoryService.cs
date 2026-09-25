using HygiaTrade.Common.Requests.Category;
using HygiaTrade.Common.Responses.Category;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public sealed class CategoryService(
    ICategoryRepository categoryRepository,
    ICategoryHierarchyPolicy hierarchyPolicy,
    ICategoryMapper mapper)
    : ICategoryService
{
    public async Task<IEnumerable<CategoryResponse>?> GetAsync()
    {
        List<Category> categories =
            (await categoryRepository.GetAllAsync())
                .Where(category => !category.IsDeleted)
                .OrderBy(category =>
                    category.ParentCategoryId.HasValue)
                .ThenBy(category =>
                    category.Name)
                .ToList();

        return mapper.ToResponses(categories);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id)
    {
        Category category =
            await GetRequiredCategoryAsync(id);

        IReadOnlyCollection<Category> categories =
            await GetActiveCategoriesAsync();

        return mapper.ToResponse(
            category,
            categories);
    }

    public async Task<CategoryResponse?> CreateAsync(
        CreateCategoryRequest request)
    {
        Guid? parentId =
            await hierarchyPolicy.ValidateParentAsync(
                request.ParentCategoryId,
                null);

        Category category = new()
        {
            Name = request.Name.Trim(),
            ImageUri = request.ImageURI,
            ParentCategoryId = parentId
        };

        Category created =
            (await categoryRepository.AddAsync(category))!;

        IReadOnlyCollection<Category> categories =
            await GetActiveCategoriesAsync();

        return mapper.ToResponse(
            created,
            categories);
    }

    public async Task<CategoryResponse?> UpdateAsync(
        UpdateCategoryRequest request)
    {
        Category existingCategory =
            await GetRequiredCategoryAsync(
                request.Id);

        Guid? parentId =
            await hierarchyPolicy.ValidateParentAsync(
                request.ParentCategoryId,
                request.Id);

        existingCategory.Name =
            request.Name.Trim();

        existingCategory.ImageUri =
            request.ImageURI;

        existingCategory.ParentCategoryId =
            parentId;

        Category updatedCategory =
            (await categoryRepository.UpdateAsync(
                existingCategory))!;

        IReadOnlyCollection<Category> categories =
            await GetActiveCategoriesAsync();

        return mapper.ToResponse(
            updatedCategory,
            categories);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await GetRequiredCategoryAsync(id);

        IReadOnlyCollection<Category> categories =
            await GetActiveCategoriesAsync();

        hierarchyPolicy.EnsureCanDelete(
            id,
            categories);

        return await categoryRepository.DeleteAsync(id);
    }

    private async Task<Category> GetRequiredCategoryAsync(
        Guid id) =>
        await categoryRepository.GetByIdAsync(id)
        ?? throw new AppException("Category not found.")
            .SetStatusCode(404);

    private async Task<IReadOnlyCollection<Category>>
        GetActiveCategoriesAsync() =>
        (await categoryRepository.GetAllAsync())
            .Where(category => !category.IsDeleted)
            .ToList();
}
