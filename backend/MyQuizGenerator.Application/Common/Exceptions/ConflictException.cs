namespace MyQuizGenerator.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when there's a conflict (409)
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message = "Conflict occurred")
        : base(message, 409)
    {
    }
}
