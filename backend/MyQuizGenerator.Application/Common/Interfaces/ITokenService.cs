using MyQuizGenerator.Application.Common.Models;

namespace MyQuizGenerator.Application.Common.Interfaces;

/// <summary>
/// Token generation service interface.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates JWT access token for user.
    /// </summary>
    string GenerateAccessToken(TokenUserInfo user, IList<string> roles);

    /// <summary>
    /// Generates random refresh token.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Returns access token expiration time.
    /// </summary>
    DateTime GetAccessTokenExpiration();

    DateTime GetRefreshTokenExpiration();

}
