namespace MyQuizGenerator.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found (404)
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message = "Resource not found")
        : base(message, 404)
    {
    }

    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found", 404)
    {
    }
}
