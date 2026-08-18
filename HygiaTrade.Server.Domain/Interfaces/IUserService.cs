using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Common.Responses.Users;

namespace HygiaTrade.Domain.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponse>?> GetAsync();
    Task<UserResponse?> GetByIdAsync(Guid id);
    Task<UserResponse?> GetCurrentUserAsync();
    Task<UserResponse?> UpdateCurrentUserAsync(UpdateCurrentUserRequest request);
    Task<UserResponse?> UpdateAsync(UpdateUserRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> PromoteToAdminAsync(RoleChangeRequest request);
    Task<bool> DemoteToRegisteredCustomerAsync(RoleChangeRequest request); 
}
