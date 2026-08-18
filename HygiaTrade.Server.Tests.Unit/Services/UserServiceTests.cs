using Moq;
using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HygiaTrade.Tests.Unit.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> userRepositoryMock;
        private readonly Mock<IAuthService> authServiceMock;
        private readonly UserService userService;

        public UserServiceTests()
        {
            userRepositoryMock = new();
            authServiceMock = new();
            userService = new(userRepositoryMock.Object, authServiceMock.Object);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnUsers()
        {
            List<User> users = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "user1@example.com",
                    Names = null,
                    Phone = null
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Email = "user2@example.com",
                    Names = null,
                    Phone = null
                }
            };

            userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            IEnumerable<UserResponse> result = await userService.GetAsync();

            Assert.NotNull(result);
            Assert.Collection(result,
                item => Assert.Equal("user1@example.com", item.Email),
                item => Assert.Equal("user2@example.com", item.Email));
        }

        [Fact]
        public async Task GetByIdAsync_UserNotFound_ShouldThrowNotFound()
        {
            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<AppException>(() => userService.GetByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingUser_ShouldReturnUser()
        {
            User user = new()
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                Names = null,
                Phone = null
            };

            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(user);

            UserResponse result = await userService.GetByIdAsync(user.Id);

            Assert.NotNull(result);
            Assert.Equal("user@example.com", result.Email);
        }

        [Fact]
        public async Task UpdateAsync_ValidUpdate_ShouldReturnUpdatedUser()
        {
            UpdateUserRequest request = new()
            {
                Id = Guid.NewGuid(),
                Email = "updated@example.com",
                Names = "Updated Name",
                Phone = "123456789"
            };

            User existingUser = new() { Id = request.Id, Email = "old@example.com", Names = "Old Name", Phone = "987654321" };
            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingUser);
            userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(new User { Id = request.Id, Email = request.Email, Names = request.Names, Phone = request.Phone });

            UserResponse result = await userService.UpdateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("updated@example.com", result.Email);
            Assert.Equal("Updated Name", result.Names);
            Assert.Equal("123456789", result.Phone);
        }

        [Fact]
        public async Task DeleteAsync_UserNotFound_ShouldThrowNotFound()
        {
            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<AppException>(() => userService.DeleteAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteAsync_ValidUser_ShouldDeleteUser()
        {
            Guid userId = Guid.NewGuid();
            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User
            {
                Id = userId,
                Email = null,
                Names = null,
                Phone = null
            });
            userRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

            bool result = await userService.DeleteAsync(userId);

            Assert.True(result);
        }

        [Fact]
        public async Task PromoteToAdminAsync_ValidRequest_ShouldPromoteToAdmin()
        {
            RoleChangeRequest request = new() { UserId = Guid.NewGuid() };

            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User
            {
                Id = request.UserId,
                Email = null,
                Names = null,
                Phone = null
            });
            userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(new User
            {
                Id = request.UserId,
                Role = "Admin",
                Email = null,
                Names = null,
                Phone = null
            });

            bool result = await userService.PromoteToAdminAsync(request);

            Assert.True(result);
        }

        [Fact]
        public async Task DemoteToRegisteredCustomerAsync_ValidRequest_ShouldDemoteToRegisteredCustomer()
        {
            RoleChangeRequest request = new() { UserId = Guid.NewGuid() };

            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new User
            {
                Id = request.UserId,
                Email = null,
                Names = null,
                Phone = null
            });
            userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync(new User
            {
                Id = request.UserId,
                Role = "RegisteredCustomer",
                Email = null,
                Names = null,
                Phone = null
            });

            bool result = await userService.DemoteToRegisteredCustomerAsync(request);

            Assert.True(result);
        }

        [Fact]
        public async Task ChangeRoleAsync_UserNotFound_ShouldThrowNotFound()
        {
            RoleChangeRequest request = new() { UserId = Guid.NewGuid() };

            userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null);

            await Assert.ThrowsAsync<AppException>(() => userService.PromoteToAdminAsync(request));
        }
    }
}
