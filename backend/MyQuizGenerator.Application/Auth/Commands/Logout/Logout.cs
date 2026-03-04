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

    public Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // Revoke refresh token from Redis — instantly invalidates the session
        return _tokenService.RevokeRefreshTokenAsync(command.Request.RefreshToken, cancellationToken);
    }
}
