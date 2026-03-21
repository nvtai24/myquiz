using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Admin.Queries.GetStatsSummary;
using MyQuizGenerator.Application.Admin.Queries.GetRevenueChart;
using MyQuizGenerator.Application.Admin.Queries.GetPlanDistribution;

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

    /// <summary>
    /// Gets revenue chart data for the last X days.
    /// </summary>
    /// <param name="days">Number of days to retrieve (e.g., 7 or 30). Defaults to 7.</param>
    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int days = 7)
    {
        var query = new GetRevenueChartQuery(days);
        var result = await Mediator.Send(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Gets plan distribution (users per plan).
    /// </summary>
    [HttpGet("plan-distribution")]
    public async Task<IActionResult> GetPlanDistribution()
    {
        var query = new GetPlanDistributionQuery();
        var result = await Mediator.Send(query);
        return ApiOk(result);
    }
}
