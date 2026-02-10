namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a confirmation email to the user.
    /// </summary>
    Task SendConfirmationEmailAsync(string userId, string email, string? firstName, string confirmationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email to the user.
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string? firstName, string resetToken, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sends a deck invitation email to the user.
    /// </summary>
    Task SendDeckInvitationEmailAsync(string email, string deckName, string token, CancellationToken cancellationToken = default);
}
