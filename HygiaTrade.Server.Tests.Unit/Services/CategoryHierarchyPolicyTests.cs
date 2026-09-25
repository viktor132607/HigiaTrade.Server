using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class CategoryHierarchyPolicyTests
{
    private readonly Mock<ICategoryRepository> repository = new();

    private CategoryHierarchyPolicy CreatePolicy() =>
        new(repository.Object);

    [Fact]
    public async Task ValidateParentAsync_ReturnsNull_WhenParentIsNotSpecified()
    {
        Guid? result =
            await CreatePolicy().ValidateParentAsync(
                null,
                null);

        Assert.Null(result);

        repository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateParentAsync_RejectsSelfParent()
    {
        Guid id = Guid.NewGuid();

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreatePolicy().ValidateParentAsync(
                    id,
                    id));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "A category cannot be its own parent.",
            ex.Message);
    }

    [Fact]
    public async Task ValidateParentAsync_Throws404_WhenParentDoesNotExist()
    {
        Guid parentId = Guid.NewGuid();

        repository
            .Setup(x => x.GetByIdAsync(parentId))
            .Returns(
                new ValueTask<Category?>(
                    (Category?)null));

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreatePolicy().ValidateParentAsync(
                    parentId,
                    null));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal(
            "Parent category not found.",
            ex.Message);
    }

    [Fact]
    public async Task ValidateParentAsync_Throws404_WhenParentIsDeleted()
    {
        Category parent =
            CategoryFor("Parent");

        parent.IsDeleted = true;

        repository
            .Setup(x => x.GetByIdAsync(parent.Id))
            .Returns(
                new ValueTask<Category?>(parent));

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreatePolicy().ValidateParentAsync(
                    parent.Id,
                    null));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task ValidateParentAsync_RejectsSecondSubcategoryLevel()
    {
        Category parent =
            CategoryFor("Child");

        parent.ParentCategoryId =
            Guid.NewGuid();

        repository
            .Setup(x => x.GetByIdAsync(parent.Id))
            .Returns(
                new ValueTask<Category?>(parent));

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreatePolicy().ValidateParentAsync(
                    parent.Id,
                    null));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Only one subcategory level is supported.",
            ex.Message);
    }

    [Fact]
    public async Task ValidateParentAsync_ReturnsValidRootParentId()
    {
        Category parent =
            CategoryFor("Parent");

        repository
            .Setup(x => x.GetByIdAsync(parent.Id))
            .Returns(
                new ValueTask<Category?>(parent));

        Guid? result =
            await CreatePolicy().ValidateParentAsync(
                parent.Id,
                Guid.NewGuid());

        Assert.Equal(parent.Id, result);
    }

    [Fact]
    public void EnsureCanDelete_Throws409_WhenActiveSubcategoryExists()
    {
        Category root =
            CategoryFor("Root");

        Category child =
            CategoryFor("Child");

        child.ParentCategoryId =
            root.Id;

        AppException ex =
            Assert.Throws<AppException>(
                () => CreatePolicy().EnsureCanDelete(
                    root.Id,
                    [root, child]));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(
            "Delete or move the subcategories first.",
            ex.Message);
    }

    [Fact]
    public void EnsureCanDelete_AllowsDelete_WhenOnlyDeletedSubcategoryExists()
    {
        Category root =
            CategoryFor("Root");

        Category child =
            CategoryFor("Child");

        child.ParentCategoryId =
            root.Id;

        child.IsDeleted = true;

        CreatePolicy().EnsureCanDelete(
            root.Id,
            [root, child]);
    }

    private static Category CategoryFor(
        string name) =>
        new()
        {
            Name = name,
            ImageUri = null
        };
}
