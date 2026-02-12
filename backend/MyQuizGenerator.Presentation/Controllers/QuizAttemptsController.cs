using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.QuizAttempts.Commands.CreateQuizAttempt;
using MyQuizGenerator.Application.QuizAttempts.DTOs;
using MyQuizGenerator.Application.QuizAttempts.Queries.GetQuizAttemptsByDeckId;
using MyQuizGenerator.Application.QuizAttempts.Queries.GetQuizAttemptById;

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

    /// <summary>
    /// Gets all quiz attempts for a specific deck by the current user.
    /// </summary>
    /// <param name="deckId">The deck ID.</param>
    /// <returns>List of quiz attempts sorted by most recent.</returns>
    [HttpGet("deck/{deckId}")]
    public async Task<IActionResult> GetByDeckId(Guid deckId)
    {
        var query = new GetQuizAttemptsByDeckIdQuery(deckId);
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Gets details of a specific quiz attempt with all user answers for review.
    /// </summary>
    /// <param name="id">The quiz attempt ID.</param>
    /// <returns>The quiz attempt details with all answers.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetQuizAttemptByIdQuery(id);
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }
}
