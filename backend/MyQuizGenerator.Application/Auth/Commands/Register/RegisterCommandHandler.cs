using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Features.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.Commands.Register;


public record RegisterCommand(
    RegisterRequest registerRequest
) : IRequest<RegisterResponse>;


public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IAuthService authService,
        IEmailService emailService,
        ILogger<RegisterCommandHandler> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.registerRequest.Email);

        // Check if user already exists
        if (await _authService.UserExistsAsync(request.registerRequest.Email))
        {
            _logger.LogWarning("Registration failed - email already exists: {Email}", request.registerRequest.Email);
            throw new ConflictException($"User with email {request.registerRequest.Email} already exists");
        }

        // Register user
        var (userId, email) = await _authService.RegisterUserAsync(
            request.registerRequest.Email,
            request.registerRequest.Password,
            request.registerRequest.FirstName,
            request.registerRequest.LastName);

        // Generate email confirmation token and send confirmation email
        try
        {
            var token = await _authService.GenerateEmailConfirmationTokenAsync(userId);
            await _emailService.SendConfirmationEmailAsync(
                userId,
                email,
                request.registerRequest.FirstName,
                token,
                cancellationToken);

            _logger.LogInformation("Confirmation email sent to: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to: {Email}", email);
            // Continue registration even if email fails - user can request resend later
        }

        // Get user info
        var roles = await _authService.GetUserRolesAsync(userId);

        var response = new RegisterResponse
        {
            User = new UserResponse
            {
                Id = userId,
                Email = email,
                FirstName = request.registerRequest.FirstName,
                LastName = request.registerRequest.LastName,
                Roles = roles.ToList()
            }
        };

        _logger.LogInformation("User registered successfully: {Email}", request.registerRequest.Email);
        return response;
    }
}
