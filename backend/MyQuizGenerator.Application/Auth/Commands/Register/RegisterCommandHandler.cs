using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;
using MyQuizGenerator.Application.Features.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.Commands.Register;


public record RegisterCommand(
    RegisterRequest registerRequest
) : IRequest<AuthResponse>;


public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        ILogger<RegisterCommandHandler> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.registerRequest.Email);

        // Check if user already exists
        if (await _authService.UserExistsAsync(request.registerRequest.Email))
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", request.registerRequest.Email);
            throw new ConflictException($"User with email {request.registerRequest.Email} already exists");
        }

        // Register user
        var (userId, email) = await _authService.RegisterUserAsync(
            request.registerRequest.Email,
            request.registerRequest.Password,
            request.registerRequest.FirstName,
            request.registerRequest.LastName);

        // Get user info and generate token
        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(userId, email, request.registerRequest.FirstName, request.registerRequest.LastName);
        var accessToken = _tokenService.GenerateAccessToken(tokenUser, roles);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
            User = new UserInfo
            {
                Id = userId,
                Email = email,
                FirstName = request.registerRequest.FirstName,
                LastName = request.registerRequest.LastName,
                Roles = roles.ToList()
            }
        };

        _logger.LogInformation("User registered successfully: {Email}", request.registerRequest.Email);
        return response;
    }
}
