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
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<LoginCommandHandler> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.loginRequest.Email);

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

        if (!await _authService.IsEmailConfirmedAsync(userId))
        {
            _logger.LogWarning("Login failed - email not confirmed: {Email}", request.loginRequest.Email);
            try
            {
                var confirmToken = await _authService.GenerateEmailConfirmationTokenAsync(userId);
                var userInfo2 = await _authService.GetUserByIdAsync(userId);
                if (userInfo2 != null)
                {
                    await _emailService.SendConfirmationEmailAsync(
                        userId, userInfo2.Email, userInfo2.FirstName, confirmToken, cancellationToken);
                    _logger.LogInformation("Confirmation email resent to: {Email}", request.loginRequest.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to: {Email}", request.loginRequest.Email);
            }
            throw new UnauthorizedException("Please confirm your email before logging in. A new confirmation email has been sent to your inbox.");
        }

        var userInfo = await _authService.GetUserByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(userId, userInfo.Email, userInfo.FirstName, userInfo.LastName);

        var accessToken = _tokenService.GenerateAccessToken(tokenUser, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token in Redis (7-day TTL)
        await _tokenService.StoreRefreshTokenAsync(refreshToken, userId, TimeSpan.FromDays(7), cancellationToken);

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
