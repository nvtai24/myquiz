using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.Auth.DTOs;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}