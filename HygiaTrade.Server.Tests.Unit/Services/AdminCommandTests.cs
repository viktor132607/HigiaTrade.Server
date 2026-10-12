using System;
using System.Linq;
using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace HygiaTrade.Server.Tests.Unit.Services;

public class AdminCommandTests
{
    [Theory]
    [InlineData("/products all price 100 quantity 100")]
    [InlineData(" /PRODUCTS ALL quantity 100 price 100,00 ")]
    public void ParsesCombinedCommand(string input)
    {
        var result = AdminCommandParser.Parse(input);
        Assert.Equal(100m, result.Price);
        Assert.Equal((uint)100, result.Quantity);
    }

    [Fact]
    public void IndividualCommandsLeaveOtherFieldUnspecified()
    {
        Assert.Null(AdminCommandParser.Parse("/products all price 0").Quantity);
        Assert.Null(AdminCommandParser.Parse("/products all quantity 0").Price);
        Assert.Equal(12.34m, AdminCommandParser.Parse("/products all price 12.34").Price);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/products all")]
    [InlineData("/products all price -1")]
    [InlineData("/products all price 1.001")]
    [InlineData("/products all price 10000000000000000")]
    [InlineData("/products all quantity 1.5")]
    [InlineData("/products all quantity -1")]
    [InlineData("/products all quantity 2147483648")]
    [InlineData("/products all price NaN")]
    [InlineData("/products all price 1e2")]
    [InlineData("/products all price 1 price 2")]
    [InlineData("/products all quantity 1 quantity 2")]
    [InlineData("/products all price 1; DROP TABLE Products")]
    [InlineData("/products all delete 1")]
    [InlineData("/products selected price 100")]
    public void RejectsInvalidCommands(string? input) =>
        Assert.Throws<ArgumentException>(() => AdminCommandParser.Parse(input));

    [Fact]
    public void EndpointRequiresAdministrator()
    {
        var authorization = typeof(AdminCommandsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(Roles.Admin, authorization.Roles);
    }
}
