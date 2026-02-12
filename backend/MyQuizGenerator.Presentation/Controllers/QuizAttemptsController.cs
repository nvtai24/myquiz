using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.QuizAttempts.Commands.CreateQuizAttempt;
using MyQuizGenerator.Application.QuizAttempts.DTOs;

namespace MyQuizGenerator.Presentation.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class QuizAttemptsController : BaseApiController
{
    private readonly IMediator _mediator;

    public QuizAttemptsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Submits a quiz attempt with user answers.
    /// </summary>
    /// <param name="request">The quiz attempt data including answers.</param>
    /// <returns>The created quiz attempt with score.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuizAttemptRequest request)
    {
        var command = new CreateQuizAttemptCommand(request);
        var result = await _mediator.Send(command);
        return ApiCreated(result, "Quiz attempt saved successfully");
    }
}
