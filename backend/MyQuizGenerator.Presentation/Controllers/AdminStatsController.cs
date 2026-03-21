using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Admin.Queries.GetStatsSummary;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Admin endpoints for dashboard statistics.
/// </summary>
[Route("api/admin/stats")]
[Authorize(Roles = "Admin")]
public class AdminStatsController : BaseApiController
{
    /// <summary>
    /// Gets a high-level summary of the application stats (Revenue, Users, Growth).
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var query = new GetStatsSummaryQuery();
        var result = await Mediator.Send(query);
        return ApiOk(result);
    }
}
