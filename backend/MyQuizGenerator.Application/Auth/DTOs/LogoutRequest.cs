namespace MyQuizGenerator.Application.Auth.DTOs;

/// <summary>
/// Logout request - tokens are read from HttpOnly cookies, body is optional for backward compatibility
/// </summary>
public class LogoutRequest
{
    public string? RefreshToken { get; set; }
    public string? AccessToken { get; set; }
}
