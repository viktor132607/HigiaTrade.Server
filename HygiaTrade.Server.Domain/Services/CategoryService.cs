using HygiaTrade.Common.Requests.Category;
using HygiaTrade.Common.Responses.Category;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public class CategoryService(ICategoryRepository categoryRepository, IImageRepository imageRepository) : ICategoryService
{
    public async Task<IEnumerable<CategoryResponse>?> GetAsync()
    {
        List<Category> categories = (await categoryRepository.GetAllAsync())
            .Where(category => !category.IsDeleted)
            .OrderBy(category => category.ParentCategoryId.HasValue)
            .ThenBy(category => category.Name)
            .ToList();

        Dictionary<Guid, string> names = categories.ToDictionary(category => category.Id, category => category.Name);
        return categories.Select(category => ToResponse(category, names));
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id)
    {
        Category? category = await categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new AppException("Category not found.").SetStatusCode(404);
        }

        Dictionary<Guid, string> names = (await categoryRepository.GetAllAsync())
            .Where(item => !item.IsDeleted)
            .ToDictionary(item => item.Id, item => item.Name);
        return ToResponse(category, names);
    }

    public async Task<CategoryResponse?> CreateAsync(CreateCategoryRequest request)
    {
        Guid? parentId = await ValidateParentAsync(request.ParentCategoryId, null);
        Category category = new()
        {
            Name = request.Name.Trim(),
            ImageUri = request.ImageURI,
            ParentCategoryId = parentId,
        };

        category = (await categoryRepository.AddAsync(category))!;
        Dictionary<Guid, string> names = (await categoryRepository.GetAllAsync())
            .Where(item => !item.IsDeleted)
            .ToDictionary(item => item.Id, item => item.Name);
        return ToResponse(category, names);
    }

    public async Task<CategoryResponse?> UpdateAsync(UpdateCategoryRequest request)
    {
        Category? existingCategory = await categoryRepository.GetByIdAsync(request.Id);
        if (existingCategory == null)
        {
            throw new AppException("Category not found.").SetStatusCode(404);
        }

        Guid? parentId = await ValidateParentAsync(request.ParentCategoryId, request.Id);
        existingCategory.Name = request.Name.Trim();
        existingCategory.ImageUri = request.ImageURI;
        existingCategory.ParentCategoryId = parentId;

        Category updatedCategory = (await categoryRepository.UpdateAsync(existingCategory))!;
        Dictionary<Guid, string> names = (await categoryRepository.GetAllAsync())
            .Where(item => !item.IsDeleted)
            .ToDictionary(item => item.Id, item => item.Name);
        return ToResponse(updatedCategory, names);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Category? category = await categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new AppException("Category not found.").SetStatusCode(404);
        }

        bool hasSubcategories = (await categoryRepository.GetAllAsync())
            .Any(item => !item.IsDeleted && item.ParentCategoryId == id);
        if (hasSubcategories)
        {
            throw new AppException("Delete or move the subcategories first.").SetStatusCode(409);
        }

        return await categoryRepository.DeleteAsync(id);
    }

    private async Task<Guid?> ValidateParentAsync(Guid? parentCategoryId, Guid? currentCategoryId)
    {
        if (!parentCategoryId.HasValue) return null;
        if (currentCategoryId.HasValue && parentCategoryId == currentCategoryId)
        {
            throw new AppException("A category cannot be its own parent.").SetStatusCode(400);
        }

        Category? parent = await categoryRepository.GetByIdAsync(parentCategoryId.Value);
        if (parent == null || parent.IsDeleted)
        {
            throw new AppException("Parent category not found.").SetStatusCode(404);
        }

        if (parent.ParentCategoryId.HasValue)
        {
            throw new AppException("Only one subcategory level is supported.").SetStatusCode(400);
        }

        return parent.Id;
    }

    private static CategoryResponse ToResponse(Category category, IReadOnlyDictionary<Guid, string> names) => new()
    {
        Id = category.Id,
        Name = category.Name,
        ImageURI = category.ImageUri,
        ParentCategoryId = category.ParentCategoryId,
        ParentCategoryName = category.ParentCategoryId.HasValue && names.TryGetValue(category.ParentCategoryId.Value, out string? parentName)
            ? parentName
            : null,
    };
}
