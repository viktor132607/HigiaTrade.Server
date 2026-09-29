using System.Data.Common;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace HygiaTrade.API.Infrastructure;

public sealed class DatabaseSessionContextInterceptor(
    IHttpContextAccessor httpContextAccessor) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) =>
        ApplyAsync(connection, CancellationToken.None).GetAwaiter().GetResult();

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default) =>
        ApplyAsync(connection, cancellationToken);

    private async Task ApplyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not NpgsqlConnection npgsql)
        {
            return;
        }

        HttpContext? context = httpContextAccessor.HttpContext;
        string userId = context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        bool isAdmin = context?.User.IsInRole("Admin") == true;
        bool isSystem = context is null;

        await using NpgsqlCommand command = npgsql.CreateCommand();
        command.CommandText = """
            SELECT
                set_config('app.current_user_id', @user_id, false),
                set_config('app.current_is_admin', @is_admin, false),
                set_config('app.current_is_system', @is_system, false);
            """;
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("is_admin", isAdmin ? "true" : "false");
        command.Parameters.AddWithValue("is_system", isSystem ? "true" : "false");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
