using System.Security.Cryptography;
using HygiaTrade.Domain.Interfaces;
using Npgsql;

namespace HygiaTrade.API.Services;

public sealed class PostgresPasswordResetTokenStore(string connectionString) : IPasswordResetTokenStore
{
    public string CreateToken(Guid userId, TimeSpan expiresIn)
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        byte[] hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

        using NpgsqlConnection connection = new(connectionString);
        connection.Open();

        using (NpgsqlCommand cleanup = new(
            """DELETE FROM "PasswordResetTokens" WHERE "ExpiresAtUtc" <= now();""",
            connection))
        {
            cleanup.ExecuteNonQuery();
        }

        using NpgsqlCommand insert = new(
            """
            INSERT INTO "PasswordResetTokens" ("TokenHash", "UserId", "ExpiresAtUtc")
            VALUES (@hash, @user_id, @expires_at);
            """,
            connection);
        insert.Parameters.AddWithValue("hash", hash);
        insert.Parameters.AddWithValue("user_id", userId);
        insert.Parameters.AddWithValue("expires_at", DateTime.UtcNow.Add(expiresIn));
        insert.ExecuteNonQuery();

        return token;
    }

    public Guid? ConsumeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        byte[] hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

        using NpgsqlConnection connection = new(connectionString);
        connection.Open();
        using NpgsqlTransaction transaction = connection.BeginTransaction();

        using NpgsqlCommand command = new(
            """
            DELETE FROM "PasswordResetTokens"
            WHERE "TokenHash" = @hash AND "ExpiresAtUtc" > now()
            RETURNING "UserId";
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("hash", hash);

        object? result = command.ExecuteScalar();
        transaction.Commit();

        return result is Guid userId ? userId : null;
    }
}
