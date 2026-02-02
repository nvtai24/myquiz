namespace MyQuizGenerator.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when user is not authenticated (401)
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(message, 401)
    {
    }
}
