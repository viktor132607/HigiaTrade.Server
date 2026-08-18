using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Repositories;

namespace HygiaTrade.Tests.Unit.Repositories;


public class WishlistRepositoryTests : RepositoryTestGenerics, IDisposable
{
    private static ApplicationDbContext _context;
    private readonly WishlistRepository _repository;

    public WishlistRepositoryTests() : base(_context)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new(options);
        _repository = new(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task IsProductAlreadyInWishlist_ShouldReturnTrue_WhenProductIsInWishlist()
    {
        WishlistItem wishlistItem = new();
        wishlistItem.Product = new() {  };
        wishlistItem.Product.Title = "Test Product";
        wishlistItem.Product.Quantity = 10;
        wishlistItem.Product.Description = "Description for test product";
        wishlistItem.Product.MainImageUrl = "http://example.com/image.jpg";
        wishlistItem.Product.Category = new() { Name = "Test Category", ImageUri = "test" };
        wishlistItem.User = new() { Email = "<EMAIL>", Names = "Test", Phone = "123456789", PasswordHash = "<PASSWORD>" };
        wishlistItem.IsDeleted = false;
        _context.WishlistItems.Add(wishlistItem);
        await _context.SaveChangesAsync();

        bool result = await _repository.IsProductAlreadyInWishlist(wishlistItem.ProductId, wishlistItem.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task IsProductAlreadyInWishlist_ShouldReturnFalse_WhenProductIsNotInWishlist()
    {
        Guid productId = Guid.NewGuid();
        Guid wishlistId = Guid.NewGuid();

        bool result = await _repository.IsProductAlreadyInWishlist(productId, wishlistId);

        Assert.False(result);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ShouldReturnWishlistItems_WhenWishlistItemsExist()
    {
        await ClearDatabaseAsync<Product>();
        await ClearDatabaseAsync<User>();
        
        User user = new()
        {
            Email = "<EMAIL>",
            Names = "Test",
            Phone = "123456789",
            PasswordHash = "<PASSWORD>"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        Guid userId = user.Id;

        Category category = new()
        {
            Name = "Test Category",
            ImageUri = "test"
        };

        Product product = new();
        product.Title = "Test Product";
        product.Quantity = 10;
        product.Description = "Description for test product";
        product.MainImageUrl = "http://example.com/image.jpg";
        product.Category = category;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        WishlistItem wishlistItem = new();
        wishlistItem.UserId = userId;
        wishlistItem.ProductId = product.Id;
        wishlistItem.IsDeleted = false;
        _context.WishlistItems.Add(wishlistItem);
        await _context.SaveChangesAsync();

        ICollection<WishlistItem> result = await _repository.GetAllByUserIdAsync(userId);

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ShouldReturnEmpty_WhenNoWishlistItemsExist()
    {
        Guid userId = Guid.NewGuid();

        ICollection<WishlistItem> result = await _repository.GetAllByUserIdAsync(userId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWishlistItemIdAsync_ShouldReturnWishlistItemId_WhenItemExistsInWishlist()
    {
        Guid userId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();
        WishlistItem wishlistItem = new();
        wishlistItem.UserId = userId;
        wishlistItem.ProductId = productId;
        wishlistItem.IsDeleted = false;
        _context.WishlistItems.Add(wishlistItem);
        await _context.SaveChangesAsync();

        Guid? result = await _repository.GetWishlistItemIdAsync(userId, productId);

        Assert.NotNull(result);
        Assert.Equal(wishlistItem.Id, result);
    }

    [Fact]
    public async Task GetWishlistItemIdAsync_ShouldReturnNull_WhenItemDoesNotExistInWishlist()
    {
        Guid userId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        Guid? result = await _repository.GetWishlistItemIdAsync(userId, productId);

        Assert.Null(result);
    }
}
