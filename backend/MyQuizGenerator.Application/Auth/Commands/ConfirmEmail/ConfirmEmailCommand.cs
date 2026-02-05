using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(ConfirmEmailRequest request) : IRequest<ConfirmEmailResponse>;



public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(
        IAuthService authService,
        ILogger<ConfirmEmailCommandHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<ConfirmEmailResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email confirmation attempt for user: {UserId}", request.request.UserId);

        // Check if email is already confirmed
        if (await _authService.IsEmailConfirmedAsync(request.request.UserId))
        {
            return new ConfirmEmailResponse
            {
                Success = true,
                Message = "Email is already confirmed."
            };
        }

        var result = await _authService.ConfirmEmailAsync(request.request.UserId, request.request.Token);

        if (!result)
        {
            _logger.LogWarning("Email confirmation failed for user: {UserId}", request.request.UserId);
            throw new ValidationException("Invalid or expired confirmation token.");
        }

        _logger.LogInformation("Email confirmed successfully for user: {UserId}", request.request.UserId);

        return new ConfirmEmailResponse
        {
            Success = true,
            Message = "Email confirmed successfully. You can now login."
        };
    }
}
