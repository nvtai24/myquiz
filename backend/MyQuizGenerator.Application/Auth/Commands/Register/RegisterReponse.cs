using MyQuizGenerator.Application.Features.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.Commands.Register;

public class RegisterResponse
{
    public UserResponse User { get; set; } = new();
    public string Message { get; set; } = "Registration successful. Please check your email to confirm your account.";
};