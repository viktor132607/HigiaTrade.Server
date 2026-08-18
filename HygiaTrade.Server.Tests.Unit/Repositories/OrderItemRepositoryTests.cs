using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Repositories;
using Xunit;

namespace HygiaTrade.Tests.Unit.Repositories;

public class OrderItemRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly OrderItemRepository _repository;

    public OrderItemRepositoryTests()
    {
        DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new();
        optionsBuilder.UseInMemoryDatabase(databaseName: "OrderItemRepositoryTestsDb");
        DbContextOptions<ApplicationDbContext> options = optionsBuilder.Options;

        _context = new(options);
        _repository = new(_context);
    }

    [Fact]
    public async Task AddRange_ShouldReturnFalse_WhenOrderItemsIsNull()
    {
        ICollection<OrderItem>? orderItems = null;
        bool result = await _repository.AddRange(orderItems);
        Assert.False(result);
    }

    [Fact]
    public async Task AddRange_ShouldReturnFalse_WhenOrderItemsIsEmpty()
    {
        ICollection<OrderItem> orderItems = new List<OrderItem>();
        bool result = await _repository.AddRange(orderItems);
        Assert.False(result);
    }

    [Fact]
    public async Task AddRange_ShouldReturnTrue_WhenOrderItemsAreValid()
    {
        List<OrderItem> orderItems = new();
        
        OrderItem orderItem = new()
        {
            OrderId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Quantity = 2,
            SinglePrice = 100m,
            TotalPrice = 200m,
            Title = "Sample Product",
            PrimaryImageUri = "http://example.com/image.jpg"
        };
        orderItems.Add(orderItem);

        bool result = await _repository.AddRange(orderItems);
        Assert.True(result);

        List<OrderItem> addedOrderItems = _context.OrderItems.ToList();
        Assert.Single(addedOrderItems);
        Assert.Equal(orderItems.First().Title, addedOrderItems.First().Title);
        Assert.Equal(orderItems.First().TotalPrice, addedOrderItems.First().TotalPrice);
    }
}
