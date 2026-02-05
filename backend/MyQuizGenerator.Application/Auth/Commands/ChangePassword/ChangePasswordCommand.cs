using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(string UserId, ChangePasswordRequest Request) : IRequest;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAuthService _authService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IAuthService authService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Change password attempt for user: {UserId}", request.UserId);

        await _authService.ChangePasswordAsync(
            request.UserId,
            request.Request.CurrentPassword,
            request.Request.NewPassword);

        _logger.LogInformation("Password changed successfully for user: {UserId}", request.UserId);
    }
}
