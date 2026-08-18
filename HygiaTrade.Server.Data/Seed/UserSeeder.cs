using Microsoft.AspNetCore.Identity;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class UserSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        PasswordHasher<User> hasher = new();

        List<User> users =
        [
            CreateUser("admin@hygiatrade.bg", "Elena Petrova", "0888123400", Roles.Admin, "Admin123!", hasher),
            CreateUser("martin.georgiev@hygiatrade.bg", "Martin Georgiev", "0888123401", Roles.RegisteredCustomer, "Customer01!", hasher),
            CreateUser("maria.dimitrova@hygiatrade.bg", "Maria Dimitrova", "0888123402", Roles.RegisteredCustomer, "Customer02!", hasher),
            CreateUser("ivan.petrov@hygiatrade.bg", "Ivan Petrov", "0888123403", Roles.RegisteredCustomer, "Customer03!", hasher),
            CreateUser("petya.stoyanova@hygiatrade.bg", "Petya Stoyanova", "0888123404", Roles.RegisteredCustomer, "Customer04!", hasher),
            CreateUser("georgi.kolev@hygiatrade.bg", "Georgi Kolev", "0888123405", Roles.RegisteredCustomer, "Customer05!", hasher),
            CreateUser("nikol.todorov@hygiatrade.bg", "Nikol Todorov", "0888123406", Roles.RegisteredCustomer, "Customer06!", hasher),
            CreateUser("borislava.ilieva@hygiatrade.bg", "Borislava Ilieva", "0888123407", Roles.RegisteredCustomer, "Customer07!", hasher),
            CreateUser("daniel.nikolov@hygiatrade.bg", "Daniel Nikolov", "0888123408", Roles.RegisteredCustomer, "Customer08!", hasher),
            CreateUser("teodora.marinova@hygiatrade.bg", "Teodora Marinova", "0888123409", Roles.RegisteredCustomer, "Customer09!", hasher),
            CreateUser("stela.atanasova@hygiatrade.bg", "Stela Atanasova", "0888123410", Roles.RegisteredCustomer, "Customer10!", hasher),
        ];

        HashSet<string> existingEmails = db.Users
            .Where(user => !string.IsNullOrWhiteSpace(user.Email))
            .Select(user => user.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<User> usersToAdd = users
            .Where(user => !existingEmails.Contains(user.Email))
            .ToList();

        if (usersToAdd.Count == 0)
        {
            return;
        }

        await db.Users.AddRangeAsync(usersToAdd);
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
