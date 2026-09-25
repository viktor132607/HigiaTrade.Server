using HygiaTrade.Common.Responses.Category;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Services;

public interface ICategoryMapper
{
    IReadOnlyList<CategoryResponse> ToResponses(
        IReadOnlyCollection<Category> categories);

    CategoryResponse ToResponse(
        Category category,
        IReadOnlyCollection<Category> categories);
}

public sealed class CategoryMapper : ICategoryMapper
{
    public IReadOnlyList<CategoryResponse> ToResponses(
        IReadOnlyCollection<Category> categories)
    {
        IReadOnlyDictionary<Guid, string> names =
            CreateNames(categories);

        return categories
            .Select(category =>
                ToResponse(category, names))
            .ToList();
    }

    public CategoryResponse ToResponse(
        Category category,
        IReadOnlyCollection<Category> categories) =>
        ToResponse(
            category,
            CreateNames(categories));

    private static IReadOnlyDictionary<Guid, string>
        CreateNames(
            IEnumerable<Category> categories) =>
        categories
            .Where(category => !category.IsDeleted)
            .ToDictionary(
                category => category.Id,
                category => category.Name);

    private static CategoryResponse ToResponse(
        Category category,
        IReadOnlyDictionary<Guid, string> names) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            ImageURI = category.ImageUri,
            ParentCategoryId =
                category.ParentCategoryId,
            ParentCategoryName =
                category.ParentCategoryId.HasValue &&
                names.TryGetValue(
                    category.ParentCategoryId.Value,
                    out string? parentName)
                    ? parentName
                    : null
        };
}
