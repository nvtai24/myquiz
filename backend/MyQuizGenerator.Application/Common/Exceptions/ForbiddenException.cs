namespace MyQuizGenerator.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when user doesn't have permission (403)
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Access denied")
        : base(message, 403)
    {
    }
}
