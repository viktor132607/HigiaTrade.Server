using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Services;

public interface IUserMapper
{
    UserResponse ToResponse(User user);
}

public sealed class UserMapper : IUserMapper
{
    public UserResponse ToResponse(User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Names = user.Names,
            Phone = user.Phone,
            Role = user.Role
        };
}
