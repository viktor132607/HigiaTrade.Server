using System.Threading.Tasks;
using Xunit;
using HygiaTrade.Data.Repositories;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Data;

namespace HygiaTrade.Tests.Unit.Repositories;

public class CategoryRepositoryTests
{
    private readonly CategoryRepository _repository;
    private readonly ApplicationDbContext _context;

    public CategoryRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        _context = new(options);
        _repository = new(_context);
    }

    [Fact]
    public async Task IsNameAlreadyUsed_ShouldReturnTrue_WhenNameIsUsed()
    {
        string name = "test@example.com";
        User existingUser = new() { Email = name, IsDeleted = false, Names = "test", Phone = "test", PasswordHash = "test" };
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();
        bool result = await _repository.IsNameAlreadyUsed(name);
        Assert.True(result);
    }

    [Fact]
    public async Task IsNameAlreadyUsed_ShouldReturnFalse_WhenNameIsNotUsed()
    {
        string name = "test@example.com";
        bool result = await _repository.IsNameAlreadyUsed(name);
        Assert.False(result);
    }

    [Fact]
    public async Task IsNameAlreadyUsed_ShouldReturnFalse_WhenUserIsDeleted()
    {
        string name = "test@example.com";
        User existingUser = new() { Email = name, IsDeleted = true, Names = "test", Phone = "test", PasswordHash = "test" };
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();
        bool result = await _repository.IsNameAlreadyUsed(name);
        Assert.False(result);
    }

    [Fact]
    public async Task IsNameAlreadyUsed_ShouldReturnFalse_WhenNameIsNullOrEmpty()
    {
        string name = "";
        bool result = await _repository.IsNameAlreadyUsed(name);
        Assert.False(result);
    }
}
