using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;

namespace MyQuizGenerator.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(RefreshTokenRequest request) : IRequest<RefreshTokenResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly ITokenService _tokenService;
    private readonly IAuthService _authService;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        ITokenService tokenService,
        IAuthService authService,
        IRefreshTokenCacheService refreshTokenCache,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _tokenService = tokenService;
        _authService = authService;
        _refreshTokenCache = refreshTokenCache;
        _logger = logger;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = request.request.RefreshToken;

        // 1. Validate: key exists in Redis → token is valid (not expired, not revoked)
        var userId = await _refreshTokenCache.GetUserIdAsync(refreshToken, cancellationToken);
        if (userId == null)
        {
            throw new ValidationException(new List<string> { "Refresh token is invalid or has expired" });
        }

        // 2. Get user
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        // 3. Token rotation: revoke old token, issue new one (prevents replay)
        await _refreshTokenCache.RemoveAsync(refreshToken, cancellationToken);

        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(user.Id, user.Email, user.FirstName, user.LastName);

        var newAccessToken = _tokenService.GenerateAccessToken(tokenUser, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        await _refreshTokenCache.StoreAsync(newRefreshToken, userId, TimeSpan.FromDays(7), cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}", userId);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
        };
    }
}
