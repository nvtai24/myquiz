namespace MyQuizGenerator.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when validation fails (422)
/// </summary>
public class ValidationException : AppException
{
    public ValidationException(string message = "Validation failed")
        : base(message, 422)
    {
    }

    public ValidationException(List<string> errors, string message = "Validation failed")
        : base(message, 422, errors)
    {
    }
}
