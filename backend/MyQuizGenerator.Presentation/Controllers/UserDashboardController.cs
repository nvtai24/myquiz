using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.User.Commands.RecordDeckView;
using MyQuizGenerator.Application.User.Queries.GetRecentDecks;
using MyQuizGenerator.Application.User.Queries.GetUserStats;

namespace MyQuizGenerator.Presentation.Controllers;

[Route("api/user/dashboard")]
[Authorize]
public class UserDashboardController : BaseApiController
{
    /// <summary>
    /// Gets stats for the authenticated user's dashboard:
    /// total study sets, quizzes attempted, average score, and current streak.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await Mediator.Send(new GetUserStatsQuery());
        return ApiOk(result);
    }

    /// <summary>
    /// Gets recently viewed, studied, or saved decks for the "Continue Studying" section.
    /// </summary>
    /// <param name="limit">Max number of decks to return (default 4).</param>
    [HttpGet("recent-decks")]
    public async Task<IActionResult> GetRecentDecks([FromQuery] int limit = 4)
    {
        var result = await Mediator.Send(new GetRecentDecksQuery(limit));
        return ApiOk(result);
    }

    /// <summary>
    /// Records that the current user has viewed a deck.
    /// Call this when the user opens a deck detail page.
    /// </summary>
    [HttpPost("decks/{deckId:guid}/view")]
    public async Task<IActionResult> RecordView(Guid deckId)
    {
        await Mediator.Send(new RecordDeckViewCommand(deckId));
        return ApiNoContent("View recorded");
    }
}
