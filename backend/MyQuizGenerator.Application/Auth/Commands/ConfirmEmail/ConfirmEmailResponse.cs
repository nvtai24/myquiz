namespace MyQuizGenerator.Application.Auth.Commands.ConfirmEmail;

public class ConfirmEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}