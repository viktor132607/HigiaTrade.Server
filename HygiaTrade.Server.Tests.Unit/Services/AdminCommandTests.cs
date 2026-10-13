using System;
using System.Linq;
using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HygiaTrade.Server.Tests.Unit.Services;

public class AdminCommandTests
{
    [Theory]
    [InlineData("/products all price 100 quantity 100")]
    [InlineData(" /PRODUCTS ALL quantity 100 price 100,00 ")]
    public void ParsesCombinedCommand(string input)
    {
        var result = AdminCommandParser.Parse(input);
        Assert.Equal(100m, result.Price);
        Assert.Equal((uint)100, result.Quantity);
    }

    [Fact]
    public void IndividualCommandsLeaveOtherFieldUnspecified()
    {
        Assert.Null(AdminCommandParser.Parse("/products all price 0").Quantity);
        Assert.Null(AdminCommandParser.Parse("/products all quantity 0").Price);
        Assert.Equal(12.34m, AdminCommandParser.Parse("/products all price 12.34").Price);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/products all")]
    [InlineData("/products all price -1")]
    [InlineData("/products all price 1.001")]
    [InlineData("/products all price 10000000000000000")]
    [InlineData("/products all quantity 1.5")]
    [InlineData("/products all quantity -1")]
    [InlineData("/products all quantity 2147483648")]
    [InlineData("/products all price NaN")]
    [InlineData("/products all price 1e2")]
    [InlineData("/products all price 1 price 2")]
    [InlineData("/products all quantity 1 quantity 2")]
    [InlineData("/products all price 1; DROP TABLE Products")]
    [InlineData("/products all delete 1")]
    [InlineData("/products selected price 100")]
    public void RejectsInvalidCommands(string? input) =>
        Assert.Throws<ArgumentException>(() => AdminCommandParser.Parse(input));

    [Fact]
    public void QuantityCommandAppliesToPackagedStockWithoutChangingPrices()
    {
        var parsed = AdminCommandParser.Parse("/products all quantity 100");
        Assert.Equal((uint)100, parsed.Quantity);
        Assert.Null(parsed.Price);
    }

    [Fact]
    public void BulkStockTargetsIncludeEveryPackagingOptionOnNonDeletedProducts()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new ApplicationDbContext(options);
        var category = new Category { Name = "Cleaning", ImageUri = null };
        var active = new Product
        {
            Title = "Dishwasher", Description = "", MainImageUrl = "",
            CategoryId = category.Id, Category = category
        };
        var inactive = new Product
        {
            Title = "Laundry", Description = "", MainImageUrl = "",
            CategoryId = category.Id, Category = category, IsActive = false
        };
        var deleted = new Product
        {
            Title = "Removed", Description = "", MainImageUrl = "",
            CategoryId = category.Id, Category = category, IsDeleted = true
        };
        db.Products.AddRange(active, inactive, deleted);
        db.ProductPackagingOptions.AddRange(
            new ProductPackagingOption { ProductId = active.Id, Label = "1 л", IsDefault = true },
            new ProductPackagingOption { ProductId = active.Id, Label = "3 л" },
            new ProductPackagingOption { ProductId = active.Id, Label = "12 × 1 л", IsActive = false },
            new ProductPackagingOption { ProductId = inactive.Id, Label = "20 × 3 л" },
            new ProductPackagingOption { ProductId = active.Id, Label = "Deleted option", IsDeleted = true },
            new ProductPackagingOption { ProductId = deleted.Id, Label = "Deleted product option" }
        );
        db.SaveChanges();

        var targeted = AdminCommandStockTargets.Packaging(db).Select(option => option.Label).ToList();

        Assert.Equal(4, targeted.Count);
        Assert.Contains("1 л", targeted);
        Assert.Contains("3 л", targeted);
        Assert.Contains("12 × 1 л", targeted);
        Assert.Contains("20 × 3 л", targeted);
        Assert.DoesNotContain("Deleted option", targeted);
        Assert.DoesNotContain("Deleted product option", targeted);
    }

    [Fact]
    public void EndpointRequiresAdministrator()
    {
        var authorization = typeof(AdminCommandsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(Roles.Admin, authorization.Roles);
    }
}
