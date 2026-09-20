using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using HygiaTrade.API.Configuration;
using Xunit;

namespace HygiaTrade.Tests.Unit.Configuration;

public class DatabaseConnectionStringResolverSafeTests
{
    [Fact]
    public void Resolve_ShouldPreferExplicitDefaultConnection_OverPlatformDatabaseUrl()
    {
        TestHostEnvironment environment = new(Environments.Production);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=dpg-new;Port=5432;Database=newdb;Username=newuser;SSL Mode=Require;",
                ["DATABASE_URL"] =
                    "Host=dpg-old;Port=5432;Database=olddb;Username=olduser;SSL Mode=Require;"
            })
            .Build();

        ResolvedDatabaseConnection result =
            DatabaseConnectionStringResolver.Resolve(configuration, environment);

        Assert.Equal("ConnectionStrings:DefaultConnection", result.SourceKey);
        Assert.Contains("Host=dpg-new", result.ConnectionString);
        Assert.Contains("Database=newdb", result.ConnectionString);
    }

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
