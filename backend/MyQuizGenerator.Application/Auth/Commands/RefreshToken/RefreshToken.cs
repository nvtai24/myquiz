using System.Security.Claims;
using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Models;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(RefreshTokenRequest request) : IRequest<RefreshTokenResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly ITokenService _tokenService;
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Guid, Domain.Entities.RefreshToken> _refreshTokenRepository;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        ITokenService tokenService,
        IAuthService authService,
        IUnitOfWork unitOfWork,
        IRepository<Guid, Domain.Entities.RefreshToken> refreshTokenRepository,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _tokenService = tokenService;
        _authService = authService;
        _unitOfWork = unitOfWork;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = request.request.RefreshToken;

        // 1. Get Refresh Token from DB
        var storedRefreshToken = _refreshTokenRepository.GetQueryable()
            .FirstOrDefault(x => x.Token == refreshToken);

        if (storedRefreshToken == null)
        {
            throw new ValidationException(new List<string> { "Refresh token does not exist" });
        }

        // 2. Validate Refresh Token
        if (storedRefreshToken.ExpiryAt < DateTime.UtcNow)
        {
            throw new ValidationException(new List<string> { "Refresh token has expired" });
        }

        if (storedRefreshToken.Invalidated)
        {
            throw new ValidationException(new List<string> { "Refresh token has been invalidated" });
        }

        if (storedRefreshToken.Used)
        {
            throw new ValidationException(new List<string> { "Refresh token has been used" });
        }

        // 3. Get User
        var userId = storedRefreshToken.UserId;
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        // 4. Mark current token as used
        storedRefreshToken.Used = true;
        _refreshTokenRepository.Update(storedRefreshToken);

        // 5. Generate new tokens
        var roles = await _authService.GetUserRolesAsync(userId);
        var tokenUser = new TokenUserInfo(user.Id, user.Email, user.FirstName, user.LastName);

        var newAccessToken = _tokenService.GenerateAccessToken(tokenUser, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // 6. Save new refresh token
        var newRefreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            JwtId = Guid.NewGuid().ToString(),
            CreationAt = DateTime.UtcNow,
            ExpiryAt = _tokenService.GetRefreshTokenExpiration(),
            Used = false,
            Invalidated = false,
            UserId = userId
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _tokenService.GetAccessTokenExpiration(),
        };
    }
}
