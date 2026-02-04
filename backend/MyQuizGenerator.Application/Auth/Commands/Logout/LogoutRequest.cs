using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.Auth.Commands.Logout;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
