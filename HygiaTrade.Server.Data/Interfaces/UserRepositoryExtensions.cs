using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public static class UserRepositoryExtensions
{
    public static Task<User?> GetByIdAsync(this IUserRepository repository, Guid? id)
        => id.HasValue ? repository.GetByIdAsync(id.Value) : Task.FromResult<User?>(null);
}
