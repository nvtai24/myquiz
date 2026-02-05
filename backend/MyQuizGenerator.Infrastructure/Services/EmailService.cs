using System.Web;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Infrastructure.Settings;

namespace MyQuizGenerator.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port,
                _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);

            // Authenticate if needed
            if (!string.IsNullOrEmpty(_emailSettings.Username))
            {
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendConfirmationEmailAsync(string userId, string email, string? firstName, string confirmationToken, CancellationToken cancellationToken = default)
    {
        var encodedToken = HttpUtility.UrlEncode(confirmationToken);
        var confirmationLink = $"{_emailSettings.ClientBaseUrl}/confirm-email?userId={userId}&token={encodedToken}";

        var emailBody = GenerateConfirmationEmailBody(firstName ?? "User", confirmationLink);

        await SendEmailAsync(
            email,
            "Confirm Your Email - MyQuiz Generator",
            emailBody,
            cancellationToken);

        _logger.LogInformation("Confirmation email sent to: {Email}", email);
    }

    private static string GenerateConfirmationEmailBody(string userName, string confirmationLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: #ffffff; margin: 0;'>MyQuiz Generator</h1>
    </div>
    <div style='background-color: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Welcome, {userName}!</h2>
        <p>Thank you for registering with MyQuiz Generator. Please confirm your email address by clicking the button below:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{confirmationLink}' 
               style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                      color: #ffffff; 
                      padding: 15px 30px; 
                      text-decoration: none; 
                      border-radius: 5px; 
                      font-weight: bold;
                      display: inline-block;'>
                Confirm Email
            </a>
        </div>
        <p style='color: #e74c3c; font-size: 14px; text-align: center; font-weight: bold;'>
            ⏰ This link will expire in 24 hours.
        </p>
        <p style='color: #666; font-size: 14px;'>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #667eea; font-size: 12px;'>{confirmationLink}</p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
        <p style='color: #999; font-size: 12px; text-align: center;'>
            If you didn't create an account, you can safely ignore this email.
        </p>
    </div>
</body>
</html>";
    }

    public async Task SendPasswordResetEmailAsync(string email, string? firstName, string resetToken, CancellationToken cancellationToken = default)
    {
        var encodedToken = HttpUtility.UrlEncode(resetToken);
        var encodedEmail = HttpUtility.UrlEncode(email);
        var resetLink = $"{_emailSettings.ClientBaseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

        var emailBody = GeneratePasswordResetEmailBody(firstName ?? "User", resetLink);

        await SendEmailAsync(
            email,
            "Reset Your Password - MyQuiz Generator",
            emailBody,
            cancellationToken);

        _logger.LogInformation("Password reset email sent to: {Email}", email);
    }

    private static string GeneratePasswordResetEmailBody(string userName, string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: #ffffff; margin: 0;'>MyQuiz Generator</h1>
    </div>
    <div style='background-color: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333;'>Password Reset Request</h2>
        <p>Hi {userName},</p>
        <p>We received a request to reset your password. Click the button below to create a new password:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{resetLink}' 
               style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                      color: #ffffff; 
                      padding: 15px 30px; 
                      text-decoration: none; 
                      border-radius: 5px; 
                      font-weight: bold;
                      display: inline-block;'>
                Reset Password
            </a>
        </div>
        <p style='color: #e74c3c; font-size: 14px; text-align: center; font-weight: bold;'>
            ⏰ This link will expire in 24 hours.
        </p>
        <p style='color: #666; font-size: 14px;'>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #667eea; font-size: 12px;'>{resetLink}</p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
        <p style='color: #999; font-size: 12px; text-align: center;'>
            If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.
        </p>
    </div>
</body>
</html>";
    }
}
