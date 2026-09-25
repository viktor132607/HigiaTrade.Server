using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class UserMapperTests
{
    [Fact]
    public void ToResponse_MapsPublicUserFields()
    {
        User user = new()
        {
            Email = "user@example.com",
            Names = "Test User",
            Phone = "123",
            Role = "Admin",
            PasswordHash = "secret"
        };

        var result =
            new UserMapper().ToResponse(user);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Names, result.Names);
        Assert.Equal(user.Phone, result.Phone);
        Assert.Equal(user.Role, result.Role);
    }
}
