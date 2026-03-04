namespace MyQuizGenerator.Application.Common.Interfaces;

/// <summary>
/// Redis-backed service for managing refresh token lifecycle.
/// A token is valid if and only if its key exists in Redis (TTL handles expiry automatically).
/// </summary>
public interface IRefreshTokenCacheService
{
    /// <summary>Stores a refresh token mapped to userId with a TTL.</summary>
    Task StoreAsync(string token, string userId, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Returns the userId for the token, or null if absent/expired/revoked.</summary>
    Task<string?> GetUserIdAsync(string token, CancellationToken ct = default);

    /// <summary>Immediately removes (revokes) a refresh token.</summary>
    Task RemoveAsync(string token, CancellationToken ct = default);
}
