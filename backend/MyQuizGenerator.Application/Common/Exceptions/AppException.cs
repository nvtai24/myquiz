namespace MyQuizGenerator.Application.Common.Exceptions;

/// <summary>
/// Base exception for all application-specific exceptions
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// HTTP Status Code associated with this exception
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// List of detailed error messages
    /// </summary>
    public List<string>? Errors { get; }

    protected AppException(string message, int statusCode = 400, List<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}
