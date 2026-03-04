using MediatR;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Auth.Commands.Logout;

public record LogoutCommand(LogoutRequest Request) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenCacheService _refreshTokenCache;

    public LogoutCommandHandler(IRefreshTokenCacheService refreshTokenCache)
    {
        _refreshTokenCache = refreshTokenCache;
    }

    public Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // Revoke by deleting the Redis key — instantly invalidates the token
        return _refreshTokenCache.RemoveAsync(command.Request.RefreshToken, cancellationToken);
    }
}
