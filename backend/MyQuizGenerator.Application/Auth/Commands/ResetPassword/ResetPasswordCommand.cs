using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IAuthService _authService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IAuthService authService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", request.Request.Email);

        var result = await _authService.ResetPasswordAsync(
            request.Request.Email,
            request.Request.Token,
            request.Request.NewPassword);

        if (!result)
        {
            _logger.LogWarning("Password reset failed for email: {Email}", request.Request.Email);
            throw new ValidationException("Invalid or expired password reset token.");
        }

        _logger.LogInformation("Password reset successfully for email: {Email}", request.Request.Email);
    }
}
