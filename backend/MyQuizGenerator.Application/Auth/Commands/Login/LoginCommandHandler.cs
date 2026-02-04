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
) : IRequest<LoginResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Guid, Domain.Entities.RefreshToken> _refreshTokenRepository;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IRepository<Guid, Domain.Entities.RefreshToken> refreshTokenRepository,
        ILogger<LoginCommandHandler> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _refreshTokenRepository = refreshTokenRepository;
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

        // Get user info and generate token
        var userInfo = await _authService.GetUserByIdAsync(userId);
        if (userInfo == null)
        {
            throw new NotFoundException("User", userId);
        }

        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(userId, userInfo.Email, userInfo.FirstName, userInfo.LastName);
        var accessToken = _tokenService.GenerateAccessToken(tokenUser, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiryDate = DateTime.UtcNow.AddDays(7); // Assuming 7 days from settings, ideally inject settings

        // Save refresh token
        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            JwtId = Guid.NewGuid().ToString(), // Should ideally extract from access token or generate
            CreationAt = DateTime.UtcNow,
            ExpiryAt = refreshTokenExpiryDate,
            Used = false,
            Invalidated = false,
            UserId = userId
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new LoginResponse
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

        _logger.LogInformation("User logged in successfully: {Email}", request.loginRequest.Email);
        return response;
    }
}
