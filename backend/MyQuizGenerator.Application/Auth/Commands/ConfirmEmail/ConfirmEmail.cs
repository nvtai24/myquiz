using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(ConfirmEmailRequest request) : IRequest;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
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

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email confirmation attempt for user: {UserId}", request.request.UserId);

        // Check if email is already confirmed
        if (await _authService.IsEmailConfirmedAsync(request.request.UserId))
        {
            _logger.LogInformation("Email is already confirmed for user: {UserId}", request.request.UserId);
            return; // Already confirmed, no error
        }

        var result = await _authService.ConfirmEmailAsync(request.request.UserId, request.request.Token);

        if (!result)
        {
            _logger.LogWarning("Email confirmation failed for user: {UserId}", request.request.UserId);
            throw new ValidationException("Invalid or expired confirmation token.");
        }

        _logger.LogInformation("Email confirmed successfully for user: {UserId}", request.request.UserId);
    }
}
