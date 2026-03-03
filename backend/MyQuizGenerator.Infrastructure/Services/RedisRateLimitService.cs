using StackExchange.Redis;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Infrastructure.Services;

public class RedisRateLimitService : IRateLimitService
{
    private readonly IDatabase _db;

    public RedisRateLimitService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    private static string BuildKey(string userId)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"gen_limit:{userId}:{today}";
    }

    /// <summary>
    /// Atomic INCR. Nếu key vừa được tạo (value == 1), set TTL đến hết ngày UTC.
    /// </summary>
    public async Task<long> IncrementDailyGenerateCountAsync(string userId, CancellationToken ct = default)
    {
        var key = BuildKey(userId);
        var count = await _db.StringIncrementAsync(key);

        if (count == 1)
        {
            // Set TTL = số giây còn lại đến 00:00:00 UTC ngày kế tiếp
            var now = DateTime.UtcNow;
            var midnight = now.Date.AddDays(1);
            var ttl = midnight - now;
            await _db.KeyExpireAsync(key, ttl);
        }

        return count;
    }

    public async Task<long> GetDailyGenerateCountAsync(string userId, CancellationToken ct = default)
    {
        var key = BuildKey(userId);
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? (long)value : 0;
    }
}
