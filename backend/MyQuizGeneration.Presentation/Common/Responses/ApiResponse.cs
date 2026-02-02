using System;
using System.Collections.Generic;

namespace MyQuizGeneration.Presentation.Common.Responses;

/// <summary>
/// Base API Response wrapper to standardize response format for Frontend
/// </summary>
/// <typeparam name="T">The type of the Data property</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Request status: true = success, false = failure
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP Status Code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Message describing the result (success or error message)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Response data (null if error occurred)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// List of detailed errors (validation errors, etc.)
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Response timestamp in UTC
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ================== Factory Methods ==================

    /// <summary>
    /// Creates a success response with data
    /// </summary>
    public static ApiResponse<T> SuccessResult(T data, string message = "Success", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failure response with error message
    /// </summary>
    public static ApiResponse<T> FailureResult(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a Created (201) response
    /// </summary>
    public static ApiResponse<T> CreatedResult(T data, string message = "Created successfully")
    {
        return SuccessResult(data, message, 201);
    }

    /// <summary>
    /// Creates a Not Found (404) response
    /// </summary>
    public static ApiResponse<T> NotFoundResult(string message = "Resource not found")
    {
        return FailureResult(message, 404);
    }

    /// <summary>
    /// Creates an Unauthorized (401) response
    /// </summary>
    public static ApiResponse<T> UnauthorizedResult(string message = "Unauthorized")
    {
        return FailureResult(message, 401);
    }

    /// <summary>
    /// Creates a Forbidden (403) response
    /// </summary>
    public static ApiResponse<T> ForbiddenResult(string message = "Access denied")
    {
        return FailureResult(message, 403);
    }

    /// <summary>
    /// Creates a Validation Error (422) response
    /// </summary>
    public static ApiResponse<T> ValidationErrorResult(List<string> errors, string message = "Validation failed")
    {
        return FailureResult(message, 422, errors);
    }

    /// <summary>
    /// Creates an Internal Server Error (500) response
    /// </summary>
    public static ApiResponse<T> InternalErrorResult(string message = "An unexpected error occurred")
    {
        return FailureResult(message, 500);
    }
}

/// <summary>
/// API Response without data (used for Delete, Update operations that don't return data)
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a success response without data
    /// </summary>
    public static ApiResponse Ok(string message = "Success", int statusCode = 200)
    {
        return new ApiResponse
        {
            Success = true,
            StatusCode = statusCode,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failure response
    /// </summary>
    public static ApiResponse Fail(string message, int statusCode = 400, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a No Content (204) response
    /// </summary>
    public static ApiResponse NoContent(string message = "No content")
    {
        return new ApiResponse
        {
            Success = true,
            StatusCode = 204,
            Message = message
        };
    }
}
