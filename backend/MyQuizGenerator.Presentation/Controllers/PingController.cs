using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Features.Ping.Queries.GetPing;

namespace MyQuizGenerator.Presentation.Controllers;

[Route("api/[controller]")]
public class PingController : BaseApiController
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Ping()
    {
        return ApiOk(new { Reply = "Pong", ServerTime = DateTime.UtcNow }, "Server is running");
    }

    /// <summary>
    /// Health check endpoint using MediatR
    /// </summary>
    [HttpGet("mediator")]
    public async Task<IActionResult> PingMediator()
    {
        var result = await Mediator.Send(new GetPingQuery());
        return ApiOk(result, "MediatR is working!");
    }

    /// <summary>
    /// Test endpoint to demonstrate exception handling - throws NotFoundException
    /// </summary>
    [HttpGet("test-not-found")]
    public IActionResult TestNotFound()
    {
        throw new NotFoundException("Quiz", 123);
    }

    /// <summary>
    /// Test endpoint to demonstrate exception handling - throws ValidationException
    /// </summary>
    [HttpGet("test-validation")]
    public IActionResult TestValidation()
    {
        var errors = new List<string> { "Field 'name' is required", "Field 'email' must be a valid email" };
        throw new ValidationException(errors);
    }

    /// <summary>
    /// Test endpoint to demonstrate exception handling - throws generic exception
    /// </summary>
    [HttpGet("test-error")]
    public IActionResult TestError()
    {
        throw new Exception("This is a test exception to demonstrate error handling");
    }
}
