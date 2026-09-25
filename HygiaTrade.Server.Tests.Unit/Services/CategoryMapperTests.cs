using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class CategoryMapperTests
{
    private readonly CategoryMapper mapper = new();

    [Fact]
    public void ToResponses_MapsParentName()
    {
        Category parent =
            CategoryFor("Parent");

        Category child =
            CategoryFor("Child");

        child.ParentCategoryId =
            parent.Id;

        var result =
            mapper.ToResponses(
                [parent, child]);

        Assert.Equal(2, result.Count);

        var childResponse =
            result.Single(x =>
                x.Id == child.Id);

        Assert.Equal(
            parent.Id,
            childResponse.ParentCategoryId);

        Assert.Equal(
            "Parent",
            childResponse.ParentCategoryName);
    }

    [Fact]
    public void ToResponse_MapsNullParentName_WhenParentIsMissing()
    {
        Category child =
            CategoryFor("Child");

        child.ParentCategoryId =
            Guid.NewGuid();

        var result =
            mapper.ToResponse(
                child,
                [child]);

        Assert.Null(
            result.ParentCategoryName);
    }

    [Fact]
    public void ToResponse_DoesNotUseDeletedParentName()
    {
        Category parent =
            CategoryFor("Parent");

        parent.IsDeleted = true;

        Category child =
            CategoryFor("Child");

        child.ParentCategoryId =
            parent.Id;

        var result =
            mapper.ToResponse(
                child,
                [parent, child]);

        Assert.Null(
            result.ParentCategoryName);
    }

    [Fact]
    public void ToResponse_MapsAllCategoryFields()
    {
        Category category =
            new()
            {
                Name = "Category",
                ImageUri = "image"
            };

        var result =
            mapper.ToResponse(
                category,
                [category]);

        Assert.Equal(category.Id, result.Id);
        Assert.Equal("Category", result.Name);
        Assert.Equal("image", result.ImageURI);
        Assert.Null(result.ParentCategoryId);
        Assert.Null(result.ParentCategoryName);
    }

    private static Category CategoryFor(
        string name) =>
        new()
        {
            Name = name,
            ImageUri = null
        };
}
