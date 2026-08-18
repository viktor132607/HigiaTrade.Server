using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using HygiaTrade.API.Configuration;
using Xunit;

namespace HygiaTrade.Tests.Unit.Configuration;

public class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void NormalizeAndValidate_ShouldReturnExistingNpgsqlConnectionString_ForDevelopment()
    {
        TestHostEnvironment environment = new TestHostEnvironment(Environments.Development);

        string result = DatabaseConnectionStringResolver.NormalizeAndValidate(
            "Host=localhost;Port=5432;Database=hygiatradedb;Username=postgres;Password=postgres;",
            "ConnectionStrings:DefaultConnection",
            environment);

        Assert.Contains("Host=localhost", result);
        Assert.Contains("Database=hygiatradedb", result);
        Assert.Contains("Username=postgres", result);
    }

    [Fact]
    public void NormalizeAndValidate_ShouldConvertPostgresUrl_ToNpgsqlConnectionString()
    {
        TestHostEnvironment environment = new TestHostEnvironment(Environments.Production);

        string result = DatabaseConnectionStringResolver.NormalizeAndValidate(
            "postgresql://hygiatrade:secret@dpg-example:5432/hygiatradedb?sslmode=require",
            "DATABASE_URL",
            environment);

        Assert.Contains("Host=dpg-example", result);
        Assert.Contains("Database=hygiatradedb", result);
        Assert.Contains("Username=hygiatrade", result);
        Assert.Contains("Password=secret", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void NormalizeAndValidate_ShouldThrow_ForMalformedConnectionString()
    {
        TestHostEnvironment environment = new TestHostEnvironment(Environments.Production);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseConnectionStringResolver.NormalizeAndValidate(
                "not-a-connection-string",
                "ConnectionStrings:DefaultConnection",
                environment));

        Assert.Contains("not a valid Npgsql connection string or postgres URL", exception.Message);
    }

    [Fact]
    public void NormalizeAndValidate_ShouldThrow_ForLoopbackHostOutsideDevelopment()
    {
        TestHostEnvironment environment = new TestHostEnvironment(Environments.Production);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseConnectionStringResolver.NormalizeAndValidate(
                "Host=localhost;Port=5432;Database=hygiatradedb;Username=postgres;Password=postgres;",
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
