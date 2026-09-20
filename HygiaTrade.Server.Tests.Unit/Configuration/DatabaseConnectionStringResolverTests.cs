using System;
using System.Collections.Generic;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using HygiaTrade.API.Configuration;
using Xunit;

namespace HygiaTrade.Tests.Unit.Configuration;

public class DatabaseConnectionStringResolverTests
{

    [Fact]
    public void Resolve_ShouldPreferExplicitDefaultConnection_OverPlatformDatabaseUrl()
    {
        TestHostEnvironment environment = new TestHostEnvironment(Environments.Production);
        Microsoft.Extensions.Configuration.IConfiguration configuration =
            new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=dpg-new;Port=5432;Database=newdb;Username=newuser;Password=newpass;SSL Mode=Require;",
                    ["DATABASE_URL"] =
                        "postgresql://olduser:oldpass@dpg-old:5432/olddb?sslmode=require"
                })
                .Build();

        ResolvedDatabaseConnection result =
            DatabaseConnectionStringResolver.Resolve(configuration, environment);

        Assert.Equal("ConnectionStrings:DefaultConnection", result.SourceKey);
        Assert.Contains("Host=dpg-new", result.ConnectionString);
        Assert.Contains("Database=newdb", result.ConnectionString);
    }

    [Fact]
    public void Resolve_ShouldPreferExplicitDefaultConnection_OverPlatformDatabaseUrl()
    {
        TestHostEnvironment environment = new TestHostEnvironment(Environments.Production);
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
