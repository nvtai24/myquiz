using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Decks.Commands.CreateDeck;
using MyQuizGenerator.Application.Decks.DTOs;
using UserDecks = MyQuizGenerator.Application.Decks.Queries.GetUserDecks;
using DeckDetails = MyQuizGenerator.Application.Decks.Queries.GetDeckById;

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

    /// <summary>
    /// Gets a list of decks for the current user.
    /// </summary>
    /// <returns>List of deck summaries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDecks.DeckSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserDecks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized();
        }

        var query = new UserDecks.GetUserDecksQuery(userId);
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Gets details of a specific deck.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    /// <returns>The deck details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DeckDetails.DeckDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeckById(Guid id)
    {
        var query = new DeckDetails.GetDeckByIdQuery(id);
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }
}
