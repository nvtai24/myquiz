using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.Auth.Commands.GoogleLogin;

/// <summary>
/// Google login request DTO.
/// </summary>
public class GoogleLoginRequest
{
    /// <summary>
    /// Google ID Token received from frontend after Google Sign-In.
    /// </summary>
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
