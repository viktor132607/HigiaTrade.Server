using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Seed;
using Xunit;

namespace HygiaTrade.Tests.Unit.Seed;

public class UserSeederTests
{
    private const string AdminPassword = "Admin123!";

    [Fact]
    public async Task SeedAsync_CreatesRequiredAdmins_WithUsablePasswordAndAdminRole()
    {
        await using ApplicationDbContext db = CreateContext();

        await UserSeeder.SeedAsync(db);

        User[] admins = await db.Users
            .Where(user =>
                user.Email == "iliev132607@gmail.com" ||
                user.Email == "higiatrade@abv.bg")
            .OrderBy(user => user.Email)
            .ToArrayAsync();

        Assert.Equal(2, admins.Length);

        PasswordHasher<User> hasher = new();

        foreach (User admin in admins)
        {
            Assert.Equal(Roles.Admin, admin.Role);
            Assert.False(admin.IsDeleted);
            Assert.Equal("0888822861", admin.Phone);

            PasswordVerificationResult result =
                hasher.VerifyHashedPassword(admin, admin.PasswordHash, AdminPassword);

            Assert.NotEqual(PasswordVerificationResult.Failed, result);
        }
    }

    [Fact]
    public async Task SeedAsync_RepairsExistingAdmin_AndRetiresLegacyAdmin()
    {
        await using ApplicationDbContext db = CreateContext();

        User existingAdmin = new()
        {
            Email = "iliev132607@gmail.com",
            Names = "Old Name",
            Phone = "0000000000",
            Role = Roles.RegisteredCustomer,
            IsDeleted = true,
            PasswordHash = "invalid-hash",
            RefreshToken = "old-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        User legacyAdmin = new()
        {
            Email = "admin@hygiatrade.bg",
            Names = "Legacy Admin",
            Phone = "0000000000",
            Role = Roles.Admin,
            IsDeleted = false,
            PasswordHash = "legacy-hash",
            RefreshToken = "legacy-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        db.Users.AddRange(existingAdmin, legacyAdmin);
        await db.SaveChangesAsync();

        await UserSeeder.SeedAsync(db);

        User repairedAdmin = await db.Users
            .SingleAsync(user => user.Email == "iliev132607@gmail.com");

        Assert.Equal(Roles.Admin, repairedAdmin.Role);
        Assert.False(repairedAdmin.IsDeleted);
        Assert.Equal("HygiaTrade Admin", repairedAdmin.Names);
        Assert.Equal("0888822861", repairedAdmin.Phone);
        Assert.Null(repairedAdmin.RefreshToken);
        Assert.Null(repairedAdmin.RefreshTokenExpiryTime);

        PasswordVerificationResult passwordResult = new PasswordHasher<User>()
            .VerifyHashedPassword(repairedAdmin, repairedAdmin.PasswordHash, AdminPassword);

        Assert.NotEqual(PasswordVerificationResult.Failed, passwordResult);

        User retiredLegacyAdmin = await db.Users
            .SingleAsync(user => user.Email == "admin@hygiatrade.bg");

        Assert.Equal(Roles.RegisteredCustomer, retiredLegacyAdmin.Role);
        Assert.True(retiredLegacyAdmin.IsDeleted);
        Assert.Null(retiredLegacyAdmin.RefreshToken);
        Assert.Null(retiredLegacyAdmin.RefreshTokenExpiryTime);
    }

    private static ApplicationDbContext CreateContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"UserSeederTests-{Guid.NewGuid()}")
                .Options;

        return new ApplicationDbContext(options);
    }
}
