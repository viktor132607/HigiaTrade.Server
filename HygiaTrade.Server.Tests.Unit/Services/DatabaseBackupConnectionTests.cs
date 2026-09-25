using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class DatabaseBackupConnectionTests
{
    [Fact]
    public void Constructor_RejectsWhitespaceConnectionString()
    {
        Assert.Throws<ArgumentException>(
            () => new DatabaseBackupConnection(" "));
    }

    [Theory]
    [InlineData("Host=localhost;Username=user")]
    [InlineData("Database=db;Username=user")]
    [InlineData("Host=localhost;Database=db")]
    public void Constructor_RequiresHostDatabaseAndUsername(
        string connectionString)
    {
        Assert.Throws<InvalidOperationException>(
            () => new DatabaseBackupConnection(
                connectionString));
    }

    [Fact]
    public void CreateEnvironment_MapsConnectionSettings()
    {
        var connection =
            new DatabaseBackupConnection(
                "Host=db.example.com;Port=5544;Database=higia;Username=app;Password=secret;SSL Mode=VerifyFull");

        IReadOnlyDictionary<string, string> env =
            connection.CreateEnvironment();

        Assert.Equal("higia", connection.Database);
        Assert.Equal("db.example.com", env["PGHOST"]);
        Assert.Equal("5544", env["PGPORT"]);
        Assert.Equal("higia", env["PGDATABASE"]);
        Assert.Equal("app", env["PGUSER"]);
        Assert.Equal("secret", env["PGPASSWORD"]);
        Assert.Equal("20", env["PGCONNECT_TIMEOUT"]);
        Assert.Equal(
            "higiatrade-database-backup",
            env["PGAPPNAME"]);
        Assert.Equal("verify-full", env["PGSSLMODE"]);
    }

    [Fact]
    public void CreateEnvironment_DoesNotExposeMissingPassword()
    {
        var connection =
            new DatabaseBackupConnection(
                "Host=localhost;Database=higia;Username=app;SSL Mode=Prefer");

        IReadOnlyDictionary<string, string> env =
            connection.CreateEnvironment();

        Assert.False(env.ContainsKey("PGPASSWORD"));
        Assert.Equal("prefer", env["PGSSLMODE"]);
    }
}
