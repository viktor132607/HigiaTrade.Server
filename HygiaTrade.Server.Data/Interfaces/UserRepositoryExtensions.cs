using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public static class UserRepositoryExtensions
{
    public static ValueTask<User?> GetByIdAsync(this IUserRepository repository, Guid? id)
        => id.HasValue ? repository.GetByIdAsync(id.Value) : ValueTask.FromResult<User?>(null);
}
