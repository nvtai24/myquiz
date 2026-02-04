namespace MyQuizGenerator.Application.Features.Auth.DTOs;

/// <summary>
/// Authentication response DTO.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT access token for API authentication.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type - always "Bearer".
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Refresh token for session renewal.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// User information.
    /// </summary>
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// Basic user information DTO.
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<string> Roles { get; set; } = new();
}
