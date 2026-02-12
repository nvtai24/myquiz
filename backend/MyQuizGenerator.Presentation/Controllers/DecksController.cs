using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Decks.DTOs;
using CreateDeck = MyQuizGenerator.Application.Decks.Commands.CreateDeck;
using UpdateDeck = MyQuizGenerator.Application.Decks.Commands.UpdateDeck;
using DeleteDeck = MyQuizGenerator.Application.Decks.Commands.DeleteDeck;
using UserDecks = MyQuizGenerator.Application.Decks.Queries.GetUserDecks;
using DeckDetails = MyQuizGenerator.Application.Decks.Queries.GetDeckById;
using MyQuizGenerator.Application.DeckInvitations.DTOs;
using MyQuizGenerator.Application.DeckInvitations.Commands.CreateDeckInvitation;

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
    /// Creates a new deck with questions and optionally attaches a file.
    /// </summary>
    /// <param name="request">The deck creation request.</param>
    /// <param name="file">Optional file to attach to the deck.</param>
    /// <returns>The ID of the created deck.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateDeckRequest request, IFormFile? file)
    {
        if (file != null && file.Length > 0)
        {
            request.FileStream = file.OpenReadStream();
            request.FileName = file.FileName;
            request.FileContentType = file.ContentType;
        }

        var command = new CreateDeck.CreateDeckCommand(request);
        var deckId = await _mediator.Send(command);
        return ApiCreated(deckId, "Deck created successfully");
    }

    /// <summary>
    /// Generates quiz questions from an uploaded file.
    /// </summary>
    /// <param name="file">The file to process (PDF, DOCX, PPTX, TXT).</param>
    /// <returns>A generated deck with questions.</returns>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using var stream = file.OpenReadStream();
        var command = new MyQuizGenerator.Application.Decks.Commands.GenerateDeckFromFiles.GenerateDeckFromFilesCommand(stream, file.FileName, file.ContentType);
        var result = await _mediator.Send(command);
        return ApiOk(result);
    }

    /// <summary>
    /// Updates a deck.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    /// <param name="request">The deck update request.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeckRequest request)
    {
        var command = new UpdateDeck.UpdateDeckCommand(id, request);
        await _mediator.Send(command);
        return ApiNoContent("Deck updated successfully");
    }

    /// <summary>
    /// Deletes a deck.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteDeck.DeleteDeckCommand(id);
        await _mediator.Send(command);
        return ApiNoContent("Deck deleted successfully");
    }

    /// <summary>
    /// Gets a list of decks for the current user.
    /// </summary>
    /// <returns>List of deck summaries.</returns>
    [HttpGet]
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
    public async Task<IActionResult> GetDeckById(Guid id)
    {
        var query = new DeckDetails.GetDeckByIdQuery(id);
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Invites a user to a deck.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    /// <param name="request">The invitation request.</param>
    [HttpPost("{id}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] MyQuizGenerator.Application.DeckInvitations.DTOs.CreateDeckInvitationRequest request)
    {
        var command = new CreateDeckInvitationCommand(id, request.Email);
        var invitationId = await _mediator.Send(command);
        return ApiCreated(invitationId, "Invitation sent successfully");
    }

    /// <summary>
    /// Accepts a deck invitation.
    /// </summary>
    /// <param name="token">The invitation token.</param>
    /// <returns>The ID of the new deck member.</returns>
    [HttpPost("invite/accept")]
    public async Task<IActionResult> AcceptInvitation([FromQuery] string token)
    {
        var command = new MyQuizGenerator.Application.DeckInvitations.Commands.AcceptDeckInvitation.AcceptDeckInvitationCommand(token);
        var memberId = await _mediator.Send(command);
        return ApiOk(memberId, "Invitation accepted successfully");
    }
}
