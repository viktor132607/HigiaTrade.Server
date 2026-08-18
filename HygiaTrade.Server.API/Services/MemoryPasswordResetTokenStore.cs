using Microsoft.Extensions.Caching.Memory;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Services;

public class MemoryPasswordResetTokenStore(IMemoryCache memoryCache) : IPasswordResetTokenStore
{
    private const string CacheKeyPrefix = "password-reset:";

    public string CreateToken(Guid userId, TimeSpan expiresIn)
    {
        string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal);

        memoryCache.Set($"{CacheKeyPrefix}{token}", userId, expiresIn);

        return token;
    }

    public Guid? ConsumeToken(string token)
    {
        string cacheKey = $"{CacheKeyPrefix}{token}";
        if (!memoryCache.TryGetValue(cacheKey, out Guid userId))
        {
            return null;
        }

        memoryCache.Remove(cacheKey);
        return userId;
    }
}
