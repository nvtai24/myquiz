using MediatR;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Auth.Commands.Logout;

public record LogoutCommand(LogoutRequest Request) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // Revoke refresh token from Redis — instantly invalidates the session
        await _tokenService.RevokeRefreshTokenAsync(command.Request.RefreshToken, cancellationToken);

        // Blacklist access token
        if (!string.IsNullOrEmpty(command.Request.AccessToken))
        {
            var (jti, exp) = _tokenService.GetAccessTokenClaims(command.Request.AccessToken);
            if (!string.IsNullOrEmpty(jti) && exp > DateTime.UtcNow)
            {
                var remaining = exp - DateTime.UtcNow;
                await _tokenService.BlacklistAccessTokenAsync(jti, remaining, cancellationToken);
            }
        }
    }
}
