using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Auth.Commands.Login;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;
using MyQuizGenerator.Application.Features.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.Commands.GoogleLogin;

/// <summary>
/// Command for Google login.
/// </summary>
public record GoogleLoginCommand(
    GoogleLoginRequest Request
) : IRequest<LoginResponse>;

/// <summary>
/// Handler for Google login command.
/// Validates Google ID token and creates/authenticates user.
/// </summary>
public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResponse>
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Guid, Domain.Entities.RefreshToken> _refreshTokenRepository;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IAuthService authService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IRepository<Guid, Domain.Entities.RefreshToken> refreshTokenRepository,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _refreshTokenRepository = refreshTokenRepository;
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
        var refreshTokenExpiryDate = _tokenService.GetRefreshTokenExpiration();

        // Save refresh token
        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            JwtId = Guid.NewGuid().ToString(),
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
