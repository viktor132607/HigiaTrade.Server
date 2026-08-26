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
            .OrderByDescending(category => category.CreatedOn)
            .ToList();

        List<CategoryResponse> response = new();
        foreach (Category category in categories)
        {
            response.Add(new()
            {
                Id = category.Id,
                Name = category.Name,
                ImageURI = category.ImageUri
            });
        }
        return response;
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid id)
    {
        Category? category = await categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new AppException("Category not found.").SetStatusCode(404);
        }
        
        return new()
        {
            Id = category.Id,
            Name = category.Name,
            ImageURI = category.ImageUri,
        };
    }

    public async Task<CategoryResponse?> CreateAsync(CreateCategoryRequest request)
    {
        Category category = new()
        {
            Name = request.Name,
            ImageUri = request.ImageURI,
        };

        category = (await categoryRepository.AddAsync(category))!;

        return new()
        {
            Id = category.Id,
            Name = category.Name,
            ImageURI = category.ImageUri,
        };
    }

    public async Task<CategoryResponse?> UpdateAsync(UpdateCategoryRequest request)
    {
        Category? existingCategory = await categoryRepository.GetByIdAsync(request.Id);
        if (existingCategory == null)
        {
            throw new AppException("Category not found.").SetStatusCode(404);
        }

        existingCategory.Name = request.Name;
        existingCategory.ImageUri = request.ImageURI;
        
        Category updatedCategory = (await categoryRepository.UpdateAsync(existingCategory))!;

        return new()
        {
            Id = updatedCategory.Id,
            Name = updatedCategory.Name,
            ImageURI = updatedCategory.ImageUri,
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Category? category = await categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new AppException("Category not found.").SetStatusCode(404);
        }
        return await categoryRepository.DeleteAsync(id);
    }
}