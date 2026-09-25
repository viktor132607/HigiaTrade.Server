using HygiaTrade.API.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class PostgresBackupToolTests
{
    private readonly Mock<IPostgresProcessRunner> runner = new();

    private PostgresBackupTool CreateTool() =>
        new(
            new DatabaseBackupConnection(
                "Host=localhost;Port=5433;Database=higia;Username=app;Password=secret;SSL Mode=Require"),
            runner.Object);

    [Fact]
    public async Task CreateBackupAsync_BuildsPgDumpRequest()
    {
        PostgresProcessRequest? captured = null;

        runner.Setup(x => x.RunAsync(
                It.IsAny<PostgresProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<PostgresProcessRequest, CancellationToken>(
                (request, _) => captured = request)
            .Returns(Task.CompletedTask);

        await CreateTool().CreateBackupAsync(
            "/tmp/a.dump",
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("pg_dump", captured!.Executable);
        Assert.Equal(
            [
                "--no-password",
                "--format=custom",
                "--compress=9",
                "--file",
                "/tmp/a.dump",
                "--dbname",
                "higia"
            ],
            captured.Arguments);
        Assert.Equal(
            "create the database backup",
            captured.Operation);
        Assert.NotNull(captured.Environment);
        Assert.Equal("secret", captured.Environment!["PGPASSWORD"]);
    }

    [Fact]
    public async Task ValidateArchiveAsync_BuildsPgRestoreListWithoutDatabaseEnvironment()
    {
        PostgresProcessRequest? captured = null;

        runner.Setup(x => x.RunAsync(
                It.IsAny<PostgresProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<PostgresProcessRequest, CancellationToken>(
                (request, _) => captured = request)
            .Returns(Task.CompletedTask);

        await CreateTool().ValidateArchiveAsync(
            "/tmp/a.dump",
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("pg_restore", captured!.Executable);
        Assert.Equal(
            ["--list", "/tmp/a.dump"],
            captured.Arguments);
        Assert.Null(captured.Environment);
        Assert.Equal(
            "validate the uploaded database backup",
            captured.Operation);
    }

    [Fact]
    public async Task RestoreBackupAsync_BuildsTransactionalCleanRestoreRequest()
    {
        PostgresProcessRequest? captured = null;

        runner.Setup(x => x.RunAsync(
                It.IsAny<PostgresProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<PostgresProcessRequest, CancellationToken>(
                (request, _) => captured = request)
            .Returns(Task.CompletedTask);

        await CreateTool().RestoreBackupAsync(
            "/tmp/a.dump",
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("pg_restore", captured!.Executable);
        Assert.Equal(
            [
                "--no-password",
                "--clean",
                "--if-exists",
                "--no-owner",
                "--no-privileges",
                "--single-transaction",
                "--exit-on-error",
                "--dbname",
                "higia",
                "/tmp/a.dump"
            ],
            captured.Arguments);
        Assert.NotNull(captured.Environment);
        Assert.Equal(
            "restore the database backup",
            captured.Operation);
    }
}
