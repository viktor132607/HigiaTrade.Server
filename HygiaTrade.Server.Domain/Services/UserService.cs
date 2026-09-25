using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IUserCurrentUserResolver currentUserResolver,
    IUserMapper mapper,
    IUserMutationFactory mutationFactory)
    : IUserService
{
    public async Task<IEnumerable<UserResponse>?> GetAsync()
    {
        IEnumerable<User> users =
            (await userRepository.GetAllAsync())
                .OrderByDescending(user =>
                    user.CreatedOn);

        return users.Select(mapper.ToResponse);
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id)
    {
        User user =
            await GetRequiredUserAsync(id);

        return mapper.ToResponse(user);
    }

    public async Task<UserResponse?> GetCurrentUserAsync()
    {
        Guid userId =
            await currentUserResolver.GetCurrentUserIdAsync();

        return await GetByIdAsync(userId);
    }

    public async Task<UserResponse?> UpdateCurrentUserAsync(
        UpdateCurrentUserRequest request)
    {
        Guid userId =
            await currentUserResolver.GetCurrentUserIdAsync();

        UpdateUserRequest updateRequest =
            mutationFactory.CreateCurrentUserUpdate(
                userId,
                request);

        return await UpdateAsync(updateRequest);
    }

    public async Task<UserResponse?> UpdateAsync(
        UpdateUserRequest request)
    {
        User existingUser =
            await GetRequiredUserAsync(request.Id);

        User updatePayload =
            mutationFactory.CreateProfileUpdate(
                existingUser,
                request);

        User? updatedUser =
            await userRepository.UpdateAsync(
                updatePayload);

        if (updatedUser is null)
        {
            throw UserErrors.NotFound();
        }

        return mapper.ToResponse(updatedUser);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await GetRequiredUserAsync(id);

        return await userRepository.DeleteAsync(id);
    }

    public Task<bool> PromoteToAdminAsync(
        RoleChangeRequest request) =>
        ChangeRoleAsync(
            request,
            Roles.Admin);

    public Task<bool> DemoteToRegisteredCustomerAsync(
        RoleChangeRequest request) =>
        ChangeRoleAsync(
            request,
            Roles.RegisteredCustomer);

    private async Task<bool> ChangeRoleAsync(
        RoleChangeRequest request,
        string toRole)
    {
        User existingUser =
            await GetRequiredUserAsync(
                request.UserId);

        User updatePayload =
            mutationFactory.CreateRoleUpdate(
                existingUser,
                toRole);

        User? updatedUser =
            await userRepository.UpdateAsync(
                updatePayload);

        return updatedUser is not null;
    }

    private async Task<User> GetRequiredUserAsync(Guid id) =>
        await userRepository.GetByIdAsync(id)
        ?? throw UserErrors.NotFound();
}
