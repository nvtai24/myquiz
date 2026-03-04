using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.Auth.DTOs;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    public string AccessToken { get; set; } = string.Empty;
}
