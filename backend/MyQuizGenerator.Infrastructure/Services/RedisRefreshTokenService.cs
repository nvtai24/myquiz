using StackExchange.Redis;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Infrastructure.Services;

/// <summary>
/// Redis implementation of <see cref="IRefreshTokenCacheService"/>.
///
/// Key pattern : rt:{token}
/// Value       : userId (plain string)
/// TTL         : set to refresh token lifetime (e.g. 7 days)
///
/// Token rotation (on refresh): DEL old key → SET new key
/// Logout / revoke            : DEL key
/// Expiry                     : handled automatically by Redis TTL
/// </summary>
public class RedisRefreshTokenService : IRefreshTokenCacheService
{
    private readonly IDatabase _db;

    public RedisRefreshTokenService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private static string Key(string token) => $"rt:{token}";

    public Task StoreAsync(string token, string userId, TimeSpan ttl, CancellationToken ct = default)
        => _db.StringSetAsync(Key(token), userId, ttl);

    public async Task<string?> GetUserIdAsync(string token, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Key(token));
        return value.HasValue ? value.ToString() : null;
    }

    public Task RemoveAsync(string token, CancellationToken ct = default)
        => _db.KeyDeleteAsync(Key(token));
}
