namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a confirmation email to the user.
    /// </summary>
    Task SendConfirmationEmailAsync(string userId, string email, string? firstName, string confirmationToken, CancellationToken cancellationToken = default);
}
