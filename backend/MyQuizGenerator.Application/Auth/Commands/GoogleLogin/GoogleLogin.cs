using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;

namespace MyQuizGenerator.Application.Auth.Commands.GoogleLogin;

/// <summary>
/// Command for Google login.
/// </summary>
public record GoogleLoginCommand(GoogleLoginRequest Request) : IRequest<LoginResponse>;

/// <summary>
/// Handler for Google login command.
/// Validates Google ID token and creates/authenticates user.
/// </summary>
public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResponse>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Google login attempt");

        // Validate token and create/get user in AuthService
        var (userId, email, firstName, lastName, isNewUser) = await _authService.GoogleLoginAsync(request.Request.IdToken);

        // Get user roles
        var roles = await _authService.GetUserRolesAsync(userId);

        // Generate tokens
        var tokenUser = new TokenUserInfo(userId, email, firstName, lastName);
        var accessToken = _tokenService.GenerateAccessToken(tokenUser, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token in Redis (7-day TTL)
        await _tokenService.StoreRefreshTokenAsync(refreshToken, userId, TimeSpan.FromDays(7), cancellationToken);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = new UserResponse
            {
                Id = userId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Roles = roles.ToList()
            }
        };

        _logger.LogInformation("Google login successful for: {Email}, IsNewUser: {IsNewUser}", email, isNewUser);
        return response;
    }
}

