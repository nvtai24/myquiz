using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
