using System.Text.Json.Serialization;

namespace MyQuizGenerator.Application.Auth.DTOs;

public class RefreshTokenResponse
{
    /// <summary>
    /// Access token - only used internally for setting cookies, not returned to client
    /// </summary>
    [JsonIgnore]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token - only used internally for setting cookies, not returned to client
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration - only used internally for setting cookies, not returned to client
    /// </summary>
    [JsonIgnore]
    public DateTime ExpiresAt { get; set; }
}
