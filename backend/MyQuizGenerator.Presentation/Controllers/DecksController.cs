using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Decks.DTOs;
using CreateDeck = MyQuizGenerator.Application.Decks.Commands.CreateDeck;
using UpdateDeck = MyQuizGenerator.Application.Decks.Commands.UpdateDeck;
using DeleteDeck = MyQuizGenerator.Application.Decks.Commands.DeleteDeck;
using UserDecks = MyQuizGenerator.Application.Decks.Queries.GetUserDecks;
using MyDecks = MyQuizGenerator.Application.Decks.Queries.GetMyDecks;
using Drafts = MyQuizGenerator.Application.Decks.Queries.GetDrafts;
using SharedDecks = MyQuizGenerator.Application.Decks.Queries.GetSharedDecks;
using AttemptedDecks = MyQuizGenerator.Application.Decks.Queries.GetAttemptedDecks;
using DeckDetails = MyQuizGenerator.Application.Decks.Queries.GetDeckById;
using SearchDecks = MyQuizGenerator.Application.Decks.Queries.SearchPublicDecks;
using MyQuizGenerator.Application.DeckInvitations.DTOs;
using MyQuizGenerator.Application.DeckInvitations.Commands.CreateDeckInvitation;
using DeckMembers = MyQuizGenerator.Application.Decks.Queries.GetDeckMembers;
using ExportDeckPdf = MyQuizGenerator.Application.Decks.Queries.ExportDeckPdf;
using SavedDecks = MyQuizGenerator.Application.Decks.Queries.GetSavedDecks;
using SaveDeck = MyQuizGenerator.Application.Decks.Commands.SaveDeck;
using UnsaveDeck = MyQuizGenerator.Application.Decks.Commands.UnsaveDeck;
using MyQuizGenerator.Application.QuizAttempts.Commands.CreateQuizAttempt;
using MyQuizGenerator.Application.QuizAttempts.DTOs;
using MyQuizGenerator.Application.QuizAttempts.Queries.GetQuizAttemptsByDeckId;
using MyQuizGenerator.Application.QuizAttempts.Queries.GetQuizAttemptById;

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
    public async Task<IActionResult> Create([FromBody] CreateDeckRequest request)
    {
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
    /// Gets a paginated list of decks for the current user.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="size">Page size (default: 10).</param>
    /// <returns>Paginated list of deck summaries.</returns>
    [HttpGet]
    public async Task<IActionResult> GetUserDecks([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized();
        }

        var query = new UserDecks.GetUserDecksQuery(userId, page, size);
        var result = await _mediator.Send(query);
        return ApiPaged(result.Items, result.Page, result.Size, result.TotalRecords);
    }

    /// <summary>
    /// Gets a paginated list of published decks owned by the current user.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="size">Page size (default: 10).</param>
    /// <returns>Paginated list of my published deck summaries.</returns>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyDecks([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized();
        }

        var query = new MyDecks.GetMyDecksQuery(userId, page, size);
        var result = await _mediator.Send(query);
        return ApiPaged(result.Items, result.Page, result.Size, result.TotalRecords);
    }

    /// <summary>
    /// Gets a paginated list of draft decks owned by the current user.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="size">Page size (default: 10).</param>
    /// <returns>Paginated list of draft deck summaries.</returns>
    [HttpGet("drafts")]
    public async Task<IActionResult> GetDrafts([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized();
        }

        var query = new Drafts.GetDraftsQuery(userId, page, size);
        var result = await _mediator.Send(query);
        return ApiPaged(result.Items, result.Page, result.Size, result.TotalRecords);
    }

    /// <summary>
    /// Gets a paginated list of decks shared with the current user.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="size">Page size (default: 10).</param>
    /// <returns>Paginated list of shared deck summaries.</returns>
    [HttpGet("shared")]
    public async Task<IActionResult> GetSharedDecks([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized();
        }

        var query = new SharedDecks.GetSharedDecksQuery(userId, page, size);
        var result = await _mediator.Send(query);
        return ApiPaged(result.Items, result.Page, result.Size, result.TotalRecords);
    }

    /// <summary>
    /// Gets a paginated list of decks the current user has attempted, sorted by most recent attempt.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="size">Page size (default: 10).</param>
    /// <returns>Paginated list of attempted deck summaries.</returns>
    [HttpGet("attempted")]
    public async Task<IActionResult> GetAttemptedDecks([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized();
        }

        var query = new AttemptedDecks.GetAttemptedDecksQuery(userId, page, size);
        var result = await _mediator.Send(query);
        return ApiPaged(result.Items, result.Page, result.Size, result.TotalRecords);
    }

    /// <summary>
    /// Searches all public decks by keyword (matches name, description, and tags).
    /// </summary>
    /// <param name="searchTerm">Optional search keyword.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="size">Page size (default: 10).</param>
    /// <returns>Paginated list of matching public decks.</returns>
    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> SearchPublicDecks([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var query = new SearchDecks.SearchPublicDecksQuery(searchTerm, page, size);
        var result = await _mediator.Send(query);
        return ApiPaged(result.Items, result.Page, result.Size, result.TotalRecords);
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
    /// Exports a deck to PDF.
    /// Requires an active subscription plan with PDF export enabled.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    /// <returns>PDF file.</returns>
    [HttpGet("{id}/export-pdf")]
    public async Task<IActionResult> ExportDeckToPdf(Guid id)
    {
        var query = new ExportDeckPdf.ExportDeckPdfQuery(id);
        var result = await _mediator.Send(query);
        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Gets all members of a deck with their full name, email, and join date.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    /// <returns>List of deck members.</returns>
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetDeckMembers(Guid id)
    {
        var query = new DeckMembers.GetDeckMembersQuery(id);
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

    /// <summary>
    /// Gets a paginated list of saved decks for the current user.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="size">Page size (default: 10).</param>
    /// <returns>Paginated list of saved deck summaries.</returns>
    [HttpGet("saved")]
    [Authorize]
    public async Task<IActionResult> GetSavedDecks([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ApiUnauthorized();
        }

        var query = new SavedDecks.GetSavedDecksQuery(userId, page, size);
        var result = await _mediator.Send(query);
        return ApiPaged(result.Items, result.Page, result.Size, result.TotalRecords);
    }

    /// <summary>
    /// Saves a deck for the current user.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    [HttpPost("{id}/save")]
    [Authorize]
    public async Task<IActionResult> SaveDeck(Guid id)
    {
        var command = new SaveDeck.SaveDeckCommand(id);
        await _mediator.Send(command);
        return ApiOk("Deck saved successfully");
    }

    /// <summary>
    /// Unsaves a deck for the current user.
    /// </summary>
    /// <param name="id">The deck ID.</param>
    [HttpDelete("{id}/save")]
    [Authorize]
    public async Task<IActionResult> UnsaveDeck(Guid id)
    {
        var command = new UnsaveDeck.UnsaveDeckCommand(id);
        await _mediator.Send(command);
        return ApiNoContent("Deck unsaved successfully");
    }

    // ── Quiz Attempts ────────────────────────────────────────────────────────

    /// <summary>
    /// Submits a quiz attempt with user answers.
    /// </summary>
    [HttpPost("attempts")]
    public async Task<IActionResult> CreateAttempt([FromBody] CreateQuizAttemptRequest request)
    {
        var result = await _mediator.Send(new CreateQuizAttemptCommand(request));
        return ApiCreated(result, "Quiz attempt saved successfully");
    }

    /// <summary>
    /// Gets all quiz attempts for a specific deck by the current user.
    /// </summary>
    [HttpGet("{deckId}/attempts")]
    public async Task<IActionResult> GetAttemptsByDeckId(Guid deckId)
    {
        var result = await _mediator.Send(new GetQuizAttemptsByDeckIdQuery(deckId));
        return ApiOk(result);
    }

    /// <summary>
    /// Gets details of a specific quiz attempt with all user answers.
    /// </summary>
    [HttpGet("attempts/{id}")]
    public async Task<IActionResult> GetAttemptById(Guid id)
    {
        var result = await _mediator.Send(new GetQuizAttemptByIdQuery(id));
        return ApiOk(result);
    }
}
