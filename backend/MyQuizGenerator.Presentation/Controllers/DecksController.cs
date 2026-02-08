using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Decks.Commands.CreateDeck;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Presentation.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DecksController : BaseApiController
{
    private readonly IMediator _mediator;

    public DecksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new deck with questions.
    /// </summary>
    /// <param name="request">The deck creation request.</param>
    /// <returns>The ID of the created deck.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeckRequest request)
    {
        var command = new CreateDeckCommand(request);
        var deckId = await _mediator.Send(command);
        return ApiCreated(deckId, "Deck created successfully");
    }
}
