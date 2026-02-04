using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Presentation.Common.Responses;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Base Controller with helper methods for ApiResponse
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    /// <summary>
    /// Returns OK (200) with data
    /// </summary>
    protected IActionResult ApiOk<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.SuccessResult(data, message));
    }

    /// <summary>
    /// Returns OK (200) without data
    /// </summary>
    protected IActionResult ApiOk(string message = "Success")
    {
        return Ok(ApiResponse.Ok(message));
    }

    /// <summary>
    /// Returns Created (201) with data
    /// </summary>
    protected IActionResult ApiCreated<T>(T data, string message = "Created successfully")
    {
        var response = ApiResponse<T>.CreatedResult(data, message);
        return StatusCode(201, response);
    }

    /// <summary>
    /// Returns No Content (204)
    /// </summary>
    protected IActionResult ApiNoContent(string message = "Deleted successfully")
    {
        return Ok(ApiResponse.NoContent(message));
    }

    /// <summary>
    /// Returns Bad Request (400)
    /// </summary>
    protected IActionResult ApiBadRequest(string message, List<string>? errors = null)
    {
        var response = ApiResponse.Fail(message, 400, errors);
        return BadRequest(response);
    }

    /// <summary>
    /// Returns Unauthorized (401)
    /// </summary>
    protected IActionResult ApiUnauthorized(string message = "Unauthorized")
    {
        var response = ApiResponse<object>.UnauthorizedResult(message);
        return Unauthorized(response);
    }

    /// <summary>
    /// Returns Forbidden (403)
    /// </summary>
    protected IActionResult ApiForbidden(string message = "Access denied")
    {
        var response = ApiResponse<object>.ForbiddenResult(message);
        return StatusCode(403, response);
    }

    /// <summary>
    /// Returns Not Found (404)
    /// </summary>
    protected IActionResult ApiNotFound(string message = "Resource not found")
    {
        var response = ApiResponse<object>.NotFoundResult(message);
        return NotFound(response);
    }

    /// <summary>
    /// Returns Validation Error (422)
    /// </summary>
    protected IActionResult ApiValidationError(List<string> errors, string message = "Validation failed")
    {
        var response = ApiResponse<object>.ValidationErrorResult(errors, message);
        return UnprocessableEntity(response);
    }

    /// <summary>
    /// Returns Internal Server Error (500)
    /// </summary>
    protected IActionResult ApiInternalError(string message = "An unexpected error occurred")
    {
        var response = ApiResponse<object>.InternalErrorResult(message);
        return StatusCode(500, response);
    }

    /// <summary>
    /// Returns Paged Response (200) with pagination
    /// </summary>
    protected IActionResult ApiPaged<T>(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalRecords,
        string message = "Success")
    {
        var response = PagedResponse<T>.Create(data, pageNumber, pageSize, totalRecords, message);
        return Ok(response);
    }
}
