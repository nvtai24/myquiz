using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.Admin.Queries.GetPaymentTransactions;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Presentation.Controllers;

/// <summary>
/// Admin endpoints for payment transaction management.
/// </summary>
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController : BaseApiController
{
    /// <summary>
    /// Gets a paginated list of payment transactions with optional search and status filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPaymentTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] PaymentStatus? status = null)
    {
        var query = new GetPaymentTransactionsQuery(page, pageSize, search, status);
        var (transactions, totalCount) = await Mediator.Send(query);
        return ApiPaged(transactions, page, pageSize, totalCount);
    }
}
