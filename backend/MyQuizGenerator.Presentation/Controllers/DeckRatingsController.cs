using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.DeckRatings.Commands.CreateDeckRating;
using MyQuizGenerator.Application.DeckRatings.DTOs;
using MyQuizGenerator.Application.DeckRatings.Queries.CheckUserDeckRating;
using MyQuizGenerator.Application.DeckRatings.Queries.GetDeckRatings;

namespace MyQuizGenerator.Presentation.Controllers;

[Route("api/decks/{deckId}/ratings")]
[ApiController]
public class DeckRatingsController : BaseApiController
{
    private readonly IMediator _mediator;

    public DeckRatingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Check if the current user has already rated a deck.
    /// </summary>
    /// <param name="deckId">The deck ID.</param>
    /// <returns>Whether the user has rated and the rating details.</returns>
    [Authorize]
    [HttpGet("check")]
    public async Task<IActionResult> CheckUserRating(Guid deckId)
    {
        var query = new CheckUserDeckRatingQuery(deckId);
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Creates a rating for a deck. Each user can only rate a deck once.
    /// </summary>
    /// <param name="deckId">The deck ID.</param>
    /// <param name="request">The rating request (rating 1-5, optional comment).</param>
    /// <returns>The ID of the rating.</returns>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateRating(Guid deckId, [FromBody] CreateDeckRatingRequest request)
    {
        var command = new CreateDeckRatingCommand(deckId, request);
        var ratingId = await _mediator.Send(command);
        return ApiCreated(ratingId, "Rating submitted successfully");
    }

    /// <summary>
    /// Gets all ratings for a deck with average rating and total count.
    /// </summary>
    /// <param name="deckId">The deck ID.</param>
    /// <returns>Deck rating summary with all individual ratings.</returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetRatings(Guid deckId)
    {
        var query = new GetDeckRatingsQuery(deckId);
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }
}
