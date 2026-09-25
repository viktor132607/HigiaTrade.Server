using HygiaTrade.Common.Requests.Category;
using HygiaTrade.Common.Responses.Category;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> repository = new();
    private readonly Mock<ICategoryHierarchyPolicy> policy = new();
    private readonly Mock<ICategoryMapper> mapper = new();

    private CategoryService CreateService() =>
        new(
            repository.Object,
            policy.Object,
            mapper.Object);

    [Fact]
    public async Task GetAsync_FiltersDeletedSortsRootsFirstThenNameAndMaps()
    {
        Category rootB = CategoryFor("B");
        Category rootA = CategoryFor("A");
        Category child = CategoryFor("C");
        child.ParentCategoryId = rootA.Id;

        Category deleted = CategoryFor("Deleted");
        deleted.IsDeleted = true;

        repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                [child, rootB, deleted, rootA]);

        IReadOnlyList<Category>? captured = null;

        mapper
            .Setup(x => x.ToResponses(
                It.IsAny<IReadOnlyCollection<Category>>()))
            .Callback<IReadOnlyCollection<Category>>(
                categories =>
                    captured = categories.ToList())
            .Returns([]);

        await CreateService().GetAsync();

        Assert.NotNull(captured);

        Assert.Equal(
            [rootA.Id, rootB.Id, child.Id],
            captured!.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByIdAsync_Throws404_WhenCategoryDoesNotExist()
    {
        Guid id = Guid.NewGuid();

        repository
            .Setup(x => x.GetByIdAsync(id))
            .Returns(new ValueTask<Category?>((Category?)null));

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().GetByIdAsync(id));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("Category not found.", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_MapsCategoryWithActiveCategorySet()
    {
        Category category = CategoryFor("Child");
        Category parent = CategoryFor("Parent");
        category.ParentCategoryId = parent.Id;

        Category deleted = CategoryFor("Deleted");
        deleted.IsDeleted = true;

        repository
            .Setup(x => x.GetByIdAsync(category.Id))
            .Returns(new ValueTask<Category?>(category));

        repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([category, parent, deleted]);

        CategoryResponse expected =
            ResponseFor(category);

        mapper
            .Setup(x => x.ToResponse(
                category,
                It.Is<IReadOnlyCollection<Category>>(
                    categories =>
                        categories.Count == 2 &&
                        categories.Contains(category) &&
                        categories.Contains(parent))))
            .Returns(expected);

        CategoryResponse? result =
            await CreateService().GetByIdAsync(
                category.Id);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task CreateAsync_ValidatesParentTrimsNamePersistsAndMaps()
    {
        Guid parentId = Guid.NewGuid();

        CreateCategoryRequest request = new()
        {
            Name = "  Child  ",
            ImageURI = "image",
            ParentCategoryId = parentId
        };

        policy
            .Setup(x => x.ValidateParentAsync(
                parentId,
                null))
            .ReturnsAsync(parentId);

        Category? captured = null;

        repository
            .Setup(x => x.AddAsync(
                It.IsAny<Category>()))
            .Callback<Category>(
                category => captured = category)
            .Returns<Category>(
                category =>
                    new ValueTask<Category?>(
                        category));

        repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                () => captured is null
                    ? []
                    : [captured]);

        mapper
            .Setup(x => x.ToResponse(
                It.IsAny<Category>(),
                It.IsAny<IReadOnlyCollection<Category>>()))
            .Returns<Category, IReadOnlyCollection<Category>>(
                (category, _) =>
                    ResponseFor(category));

        CategoryResponse? result =
            await CreateService().CreateAsync(request);

        Assert.NotNull(captured);
        Assert.Equal("Child", captured!.Name);
        Assert.Equal("image", captured.ImageUri);
        Assert.Equal(parentId, captured.ParentCategoryId);

        Assert.NotNull(result);
        Assert.Equal("Child", result!.Name);
    }

    [Fact]
    public async Task UpdateAsync_UsesValidatedParentAndMutatesExistingEntity()
    {
        Category existing = CategoryFor("Old");

        Guid parentId = Guid.NewGuid();

        UpdateCategoryRequest request = new()
        {
            Id = existing.Id,
            Name = "  New  ",
            ImageURI = "new-image",
            ParentCategoryId = parentId
        };

        repository
            .Setup(x => x.GetByIdAsync(existing.Id))
            .Returns(new ValueTask<Category?>(existing));

        policy
            .Setup(x => x.ValidateParentAsync(
                parentId,
                existing.Id))
            .ReturnsAsync(parentId);

        repository
            .Setup(x => x.UpdateAsync(existing))
            .Returns(new ValueTask<Category?>(existing));

        repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([existing]);

        mapper
            .Setup(x => x.ToResponse(
                existing,
                It.IsAny<IReadOnlyCollection<Category>>()))
            .Returns(() => ResponseFor(existing));

        CategoryResponse? result =
            await CreateService().UpdateAsync(request);

        Assert.Equal("New", existing.Name);
        Assert.Equal("new-image", existing.ImageUri);
        Assert.Equal(parentId, existing.ParentCategoryId);

        Assert.NotNull(result);
        Assert.Equal("New", result!.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ValidatesHierarchyAndReturnsRepositoryResult(
        bool deleteResult)
    {
        Category category = CategoryFor("Category");

        repository
            .Setup(x => x.GetByIdAsync(category.Id))
            .Returns(new ValueTask<Category?>(category));

        repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([category]);

        repository
            .Setup(x => x.DeleteAsync(category.Id))
            .ReturnsAsync(deleteResult);

        bool result =
            await CreateService().DeleteAsync(
                category.Id);

        Assert.Equal(deleteResult, result);

        policy.Verify(
            x => x.EnsureCanDelete(
                category.Id,
                It.IsAny<IEnumerable<Category>>()),
            Times.Once);
    }

    private static Category CategoryFor(
        string name) =>
        new()
        {
            Name = name,
            ImageUri = null
        };

    private static CategoryResponse ResponseFor(
        Category category) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            ImageURI = category.ImageUri,
            ParentCategoryId =
                category.ParentCategoryId
        };
}
