using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;

namespace MyQuizGenerator.Application.Auth.Commands.Login;

public record LoginCommand(LoginRequest loginRequest) : IRequest<LoginResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        IEmailService emailService,
        IRefreshTokenCacheService refreshTokenCache,
        ILogger<LoginCommandHandler> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _emailService = emailService;
        _refreshTokenCache = refreshTokenCache;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.loginRequest.Email);

        // Check password
        var (success, userId, isLockedOut) = await _authService.CheckPasswordAsync(
            request.loginRequest.Email,
            request.loginRequest.Password);

        if (isLockedOut)
        {
            _logger.LogWarning("Login failed - account locked: {Email}", request.loginRequest.Email);
            throw new UnauthorizedException("Account is temporarily locked due to multiple failed login attempts.");
        }

        if (!success || userId == null)
        {
            _logger.LogWarning("Login failed - invalid credentials: {Email}", request.loginRequest.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        // Check if email is confirmed
        if (!await _authService.IsEmailConfirmedAsync(userId))
        {
            _logger.LogWarning("Login failed - email not confirmed: {Email}", request.loginRequest.Email);

            // Auto-resend confirmation email
            try
            {
                var token = await _authService.GenerateEmailConfirmationTokenAsync(userId);
                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    await _emailService.SendConfirmationEmailAsync(
                        userId,
                        user.Email,
                        user.FirstName,
                        token,
                        cancellationToken);
                    _logger.LogInformation("Confirmation email resent to: {Email}", request.loginRequest.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to: {Email}", request.loginRequest.Email);
            }

            throw new UnauthorizedException("Please confirm your email before logging in. A new confirmation email has been sent to your inbox.");
        }

        // Get user info and generate tokens
        var userInfo = await _authService.GetUserByIdAsync(userId);
        if (userInfo == null)
        {
            throw new NotFoundException("User", userId);
        }

        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(userId, userInfo.Email, userInfo.FirstName, userInfo.LastName);
        var accessToken = _tokenService.GenerateAccessToken(tokenUser, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token in Redis with 7-day TTL
        await _refreshTokenCache.StoreAsync(refreshToken, userId, TimeSpan.FromDays(7), cancellationToken);

        _logger.LogInformation("User logged in successfully: {Email}", request.loginRequest.Email);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = new UserResponse
            {
                Id = userId,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Roles = roles.ToList()
            }
        };
    }
}
