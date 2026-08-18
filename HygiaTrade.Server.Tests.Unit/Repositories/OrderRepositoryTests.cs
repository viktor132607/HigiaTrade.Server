using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using Xunit;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Repositories;

namespace HygiaTrade.Tests.Unit.Repositories;

public class OrderRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new(options);
        _repository = new(_context);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        User user = new()
        {
            Email = null,
            Names = null,
            Phone = null
        };
        user.Email = "<EMAIL>";
        user.Names = "test";
        user.Phone = "test";
        user.PasswordHash = "<PASSWORD>";
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        Guid userId = user.Id;

        Order order = new();
        order.UserId = userId;
        order.Status = OrderStatus.Created;
        order.IsDeleted = false;
        order.Items = new List<OrderItem>();
        OrderItem orderItem = new()
        {
            PrimaryImageUri = "http://example.com/image.jpg",
            Title = "Sample Product",
            Quantity = 0,
            SinglePrice = 0,
            TotalPrice = 0
        };
        order.Items.Add(orderItem);

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        try
        {
            Order? result = await _repository.GetByUserIdAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(OrderStatus.Created, result.Status);
            Assert.False(result.IsDeleted);
            Assert.NotNull(result.Items);
            Assert.NotNull(result.User);
        }
        finally
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        Guid userId = Guid.NewGuid();

        Order? result = await _repository.GetByUserIdAsync(userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdWithoutStatusRestrictionAsync_ShouldReturnOrder_WhenOrderExists()
    {
        User user = new()
        {
            Email = "<EMAIL>",
            Names = "test",
            Phone = "test",
            PasswordHash = "<PASSWORD>"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        Order order = new();
        order.UserId = user.Id;
        order.Status = OrderStatus.Created;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        Order? result = await _repository.GetByUserIdWithoutStatusRestrictionAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result?.UserId);
    }

    [Fact]
    public async Task AddAsync_ShouldAddOrder_WhenCalled()
    {
        User user = new()
        {
            Email = "<EMAIL>",
            Names = "test",
            Phone = "test",
            PasswordHash = "<PASSWORD>"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        Order result = await _repository.AddAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldUpdateOrderStatus_WhenOrderExists()
    {
        Order order = new();
        order.Status = OrderStatus.Created;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        Order result = await _repository.ChangeStatusAsync(order.Id, OrderStatus.Shipped);

        Assert.Equal(OrderStatus.Shipped, result.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowException_WhenOrderNotFound()
    {
        Guid orderId = Guid.NewGuid();

        await Assert.ThrowsAsync<AppException>(() => _repository.ChangeStatusAsync(orderId, OrderStatus.Shipped));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOrder_WhenOrderExists()
    {
        Guid orderId = Guid.NewGuid();
        Order order = new();
        order.Status = OrderStatus.Created;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        order.Status = OrderStatus.Shipped;
        Order? result = await _repository.UpdateAsync(order);

        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Shipped, result?.Status);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        Order order = new();
        order.Status = OrderStatus.Created;

        Order? result = await _repository.UpdateAsync(order);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowException_WhenUserDoesNotExist()
    {
        Guid userId = Guid.NewGuid();

        await Assert.ThrowsAsync<AppException>(() => _repository.AddAsync(userId));
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenOrderIsDeleted()
    {
        User user = new()
        {
            Email = "<EMAIL>",
            Names = "test",
            Phone = "test",
            PasswordHash = "<PASSWORD>"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        Order order = new();
        order.UserId = user.Id;
        order.Status = OrderStatus.Created;
        order.IsDeleted = true;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        Order? result = await _repository.GetByUserIdAsync(user.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowException_WhenOrderNotFound_Extended()
    {
        Guid orderId = Guid.NewGuid();

        await Assert.ThrowsAsync<AppException>(() => _repository.ChangeStatusAsync(orderId, OrderStatus.Shipped));
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenOrderDoesNotExist_Extended()
    {
        Order order = new();
        order.Status = OrderStatus.Created;

        Order? result = await _repository.UpdateAsync(order);

        Assert.Null(result);
    }

    [Fact(Skip = "Skipping this test for now")]
    public async Task ChangeStatusAsync_ShouldNotAllowInvalidStatusChange()
    {
        Guid orderId = Guid.NewGuid();
        Order order = new();
        order.Status = OrderStatus.Created;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => _repository.ChangeStatusAsync(orderId, OrderStatus.Created));
    }
}
