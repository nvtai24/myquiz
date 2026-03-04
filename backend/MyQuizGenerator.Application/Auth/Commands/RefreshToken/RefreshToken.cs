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
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        ITokenService tokenService,
        IAuthService authService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _tokenService = tokenService;
        _authService = authService;
        _logger = logger;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = request.request.RefreshToken;

        // 1. Validate — key absence means expired or revoked
        var userId = await _tokenService.GetUserIdFromRefreshTokenAsync(refreshToken, cancellationToken);
        if (userId == null)
            throw new ValidationException(new List<string> { "Refresh token is invalid or has expired" });

        // 2. Get user
        var user = await _authService.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        // 3. Token rotation: revoke old → issue new (prevents replay attacks)
        await _tokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);

        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(user.Id, user.Email, user.FirstName, user.LastName);

        var newAccessToken = _tokenService.GenerateAccessToken(tokenUser, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        await _tokenService.StoreRefreshTokenAsync(newRefreshToken, userId, TimeSpan.FromDays(7), cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}", userId);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
        };
    }
}
