using MyQuizGenerator.Application.Common.Models;

namespace MyQuizGenerator.Application.Common.Interfaces;

/// <summary>
/// Central token service — handles ALL token operations:
/// - JWT access token generation
/// - Refresh token lifecycle (store / retrieve / revoke) backed by Redis
/// </summary>
public interface ITokenService
{
    // ── Access token ──────────────────────────────────────────────────────────

    /// <summary>Generates a signed JWT access token.</summary>
    string GenerateAccessToken(TokenUserInfo user, IList<string> roles);

    /// <summary>Returns the access token expiration timestamp.</summary>
    DateTime GetAccessTokenExpiration();

    /// <summary>
    /// Reads JTI ("jti" claim) and expiry time from a raw access token
    /// without full signature validation. Used during logout.
    /// </summary>
    (string Jti, DateTime Exp) GetAccessTokenClaims(string accessToken);

    // ── Refresh token (Redis) ─────────────────────────────────────────────────

    /// <summary>Generates a random opaque refresh token.</summary>
    string GenerateRefreshToken();

    /// <summary>Returns the refresh token expiration timestamp.</summary>
    DateTime GetRefreshTokenExpiration();

    /// <summary>Stores a refresh token → userId mapping in Redis with the given TTL.</summary>
    Task StoreRefreshTokenAsync(string token, string userId, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Returns the userId associated with the token, or null if absent / expired.</summary>
    Task<string?> GetUserIdFromRefreshTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Immediately removes (revokes) a refresh token from Redis.</summary>
    Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default);

    // ── Access token blacklist (Redis) ────────────────────────────────────────

    /// <summary>
    /// Adds an access token JTI to the Redis blacklist.
    /// TTL should equal the token's remaining lifetime.
    /// </summary>
    Task BlacklistAccessTokenAsync(string jti, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Returns true when the JTI is on the blacklist.</summary>
    Task<bool> IsAccessTokenBlacklistedAsync(string jti, CancellationToken ct = default);
}
