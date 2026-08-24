using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class UserSeeder
{
    private const string LegacyAdminEmail = "admin@hygiatrade.bg";
    private const string AdminPassword = "Admin123!";
    private const string AdminPhone = "0888822861";

    private static readonly (string Email, string Names)[] AdminUsers =
    [
        ("iliev132607@gmail.com", "HygiaTrade Admin"),
        ("higiatrade@abv.bg", "HygiaTrade Admin"),
    ];

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        PasswordHasher<User> hasher = new();
        await RetireLegacyAdminAsync(db);
        await EnsureAdminsAsync(db, hasher);
    }

    private static async Task RetireLegacyAdminAsync(ApplicationDbContext db)
    {
        User? legacyAdmin = await db.Users
            .FirstOrDefaultAsync(user => user.Email.ToLower() == LegacyAdminEmail);

        if (legacyAdmin is null)
        {
            return;
        }

        legacyAdmin.Role = Roles.RegisteredCustomer;
        legacyAdmin.IsDeleted = true;
        legacyAdmin.RefreshToken = null;
        legacyAdmin.RefreshTokenExpiryTime = null;
        legacyAdmin.ModifiedOn = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private static async Task EnsureAdminsAsync(
        ApplicationDbContext db,
        PasswordHasher<User> hasher)
    {
        foreach ((string email, string names) in AdminUsers)
        {
            string normalizedEmail = email.ToLowerInvariant();

            User? admin = await db.Users
                .FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);

            if (admin is null)
            {
                await db.Users.AddAsync(
                    CreateUser(email, names, AdminPhone, Roles.Admin, AdminPassword, hasher));
                continue;
            }

            admin.Email = email;
            admin.Names = names;
            admin.Phone = AdminPhone;
            admin.Role = Roles.Admin;
            admin.IsDeleted = false;
            admin.RefreshToken = null;
            admin.RefreshTokenExpiryTime = null;
            admin.ModifiedOn = DateTime.UtcNow;
            admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);
        }

        await db.SaveChangesAsync();
    }

    private static User CreateUser(
        string email,
        string names,
        string phone,
        string role,
        string password,
        PasswordHasher<User> hasher)
    {
        User user = new()
        {
            Email = email,
            PasswordHash = "temporaryPasswordHash",
            Names = names,
            Phone = phone,
            Role = role,
        };

        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }
}
