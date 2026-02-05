using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IAuthService authService,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", request.Request.Email);

        // Generate password reset token
        var token = await _authService.GeneratePasswordResetTokenAsync(request.Request.Email);

        if (token == null)
        {
            // Don't reveal that the user doesn't exist for security - return silently
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Request.Email);
            return;
        }

        // Get user info for personalization
        var user = await _authService.GetUserByEmailAsync(request.Request.Email);

        // Send password reset email
        try
        {
            await _emailService.SendPasswordResetEmailAsync(
                request.Request.Email,
                user?.FirstName,
                token,
                cancellationToken);

            _logger.LogInformation("Password reset email sent to: {Email}", request.Request.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to: {Email}", request.Request.Email);
            throw new BadRequestException("Failed to send password reset email. Please try again later.");
        }
    }
}
