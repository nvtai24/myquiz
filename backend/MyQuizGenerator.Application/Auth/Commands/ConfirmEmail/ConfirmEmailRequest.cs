using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.Auth.Commands.ConfirmEmail;

public class ConfirmEmailRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    [Required]
    public string Token { get; set; } = string.Empty;
}