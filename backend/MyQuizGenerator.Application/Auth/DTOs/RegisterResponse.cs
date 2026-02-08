using MyQuizGenerator.Application.Auth.DTOs;

namespace MyQuizGenerator.Application.Auth.DTOs;

public class RegisterResponse
{
    public UserResponse User { get; set; } = new();
};