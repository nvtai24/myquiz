using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;
using MyQuizGenerator.Infrastructure.Settings;
using StackExchange.Redis;

namespace MyQuizGenerator.Infrastructure.Identity;

/// <summary>
/// Central token service — JWT generation + Redis-backed refresh token lifecycle.
///
/// Redis key pattern: rt:{token}  →  userId   (TTL = refresh token lifetime)
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IDatabase _redis;

    private static string RefreshKey(string token) => $"rt:{token}";

    public TokenService(IOptions<JwtSettings> jwtSettings, IConnectionMultiplexer redis)
    {
        _jwtSettings = jwtSettings.Value;
        _redis = redis.GetDatabase();
    }

    // ── Access token ──────────────────────────────────────────────────────────

    public string GenerateAccessToken(TokenUserInfo user, IList<string> roles)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname, user.LastName ?? string.Empty),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: GetAccessTokenExpiration(),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetAccessTokenExpiration()
        => DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

    public (string Jti, DateTime Exp) GetAccessTokenClaims(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken))
            return (string.Empty, DateTime.MinValue);

        var jwt = handler.ReadJwtToken(accessToken);
        var jti = jwt.Id ?? string.Empty;
        var exp = jwt.ValidTo; // UTC
        return (jti, exp);
    }

    // ── Refresh token (Redis) ─────────────────────────────────────────────────

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public DateTime GetRefreshTokenExpiration()
        => DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

    public Task StoreRefreshTokenAsync(string token, string userId, TimeSpan ttl, CancellationToken ct = default)
        => _redis.StringSetAsync(RefreshKey(token), userId, ttl);

    public async Task<string?> GetUserIdFromRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        var value = await _redis.StringGetAsync(RefreshKey(token));
        return value.HasValue ? value.ToString() : null;
    }

    public Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default)
        => _redis.KeyDeleteAsync(RefreshKey(token));

    // ── Access token blacklist (Redis) ────────────────────────────────────────

    private static string BlacklistKey(string jti) => $"bl:{jti}";

    public Task BlacklistAccessTokenAsync(string jti, TimeSpan ttl, CancellationToken ct = default)
        => _redis.StringSetAsync(BlacklistKey(jti), "1", ttl);

    public async Task<bool> IsAccessTokenBlacklistedAsync(string jti, CancellationToken ct = default)
        => await _redis.KeyExistsAsync(BlacklistKey(jti));
}
