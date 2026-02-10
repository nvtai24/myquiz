namespace MyQuizGenerator.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }

    string? Email { get; }
}
