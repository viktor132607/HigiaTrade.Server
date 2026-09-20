using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using HygiaTrade.API.Configuration;
using Xunit;

namespace HygiaTrade.Tests.Unit.Configuration;

public class DatabaseConnectionStringResolverSafeTests
{
    [Fact]
    public void NormalizeAndValidate_ShouldRejectLoopbackOutsideDevelopment()
    {
        TestHostEnvironment environment = new(Environments.Production);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseConnectionStringResolver.NormalizeAndValidate(
                "Host=localhost;Port=5432;Database=testdb;Username=testuser;",
                "ConnectionStrings:DefaultConnection",
                environment));

        Assert.Contains("local development placeholder", exception.Message);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "HygiaTrade.Tests.Unit";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
