using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Decks.Queries.GetUserQuota;

namespace MyQuizGenerator.Presentation.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class QuotaController : BaseApiController
{
    private readonly IMediator _mediator;

    public QuotaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the current user's remaining quota for AI generation and deck creation.
    /// </summary>
    /// <returns>Quota information including daily generate remaining and deck slots remaining.</returns>
    [HttpGet]
    public async Task<IActionResult> GetQuota()
    {
        var query = new GetUserQuotaQuery();
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }
}
