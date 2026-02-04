using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;
using MyQuizGenerator.Application.Features.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.Commands.Login;

/// <summary>
/// Handler for user login command.
/// </summary>


public record LoginCommand(
    LoginRequest loginRequest
) : IRequest<AuthResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
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

        // Get user info and generate token
        var userInfo = await _authService.GetUserByIdAsync(userId);
        if (userInfo == null)
        {
            throw new NotFoundException("User", userId);
        }

        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(userId, userInfo.Email, userInfo.FirstName, userInfo.LastName);
        var accessToken = _tokenService.GenerateAccessToken(tokenUser, roles);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = new UserInfo
            {
                Id = userId,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Roles = roles.ToList()
            }
        };

        _logger.LogInformation("User logged in successfully: {Email}", request.loginRequest.Email);
        return response;
    }
}
