namespace MyQuizGenerator.Application.Common.Exceptions;

/// <summary>
/// Exception thrown for bad requests (400)
/// </summary>
public class BadRequestException : AppException
{
    public BadRequestException(string message = "Bad request")
        : base(message, 400)
    {
    }
}
