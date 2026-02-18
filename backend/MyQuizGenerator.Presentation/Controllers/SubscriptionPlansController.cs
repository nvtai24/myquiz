using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyQuizGenerator.Application.SubscriptionPlans.DTOs;
using CreatePlan = MyQuizGenerator.Application.SubscriptionPlans.Commands.CreateSubscriptionPlan;
using UpdatePlan = MyQuizGenerator.Application.SubscriptionPlans.Commands.UpdateSubscriptionPlan;
using GetPlans = MyQuizGenerator.Application.SubscriptionPlans.Queries.GetSubscriptionPlans;
using GetActivePlans = MyQuizGenerator.Application.SubscriptionPlans.Queries.GetActiveSubscriptionPlans;

namespace MyQuizGenerator.Presentation.Controllers;

[Route("api/subscription-plans")]
[ApiController]
public class SubscriptionPlansController : BaseApiController
{
    private readonly IMediator _mediator;

    public SubscriptionPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets active subscription plans for user display.
    /// </summary>
    /// <returns>List of active subscription plans with basic info.</returns>
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var query = new GetActivePlans.GetActiveSubscriptionPlansQuery();
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Gets all subscription plans for admin configuration.
    /// </summary>
    /// <returns>List of all subscription plans with full details.</returns>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetPlans.GetSubscriptionPlansQuery();
        var result = await _mediator.Send(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Creates a new subscription plan.
    /// </summary>
    /// <param name="request">The subscription plan creation request.</param>
    /// <returns>The created subscription plan.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequest request)
    {
        var command = new CreatePlan.CreateSubscriptionPlanCommand(request);
        var result = await _mediator.Send(command);
        return ApiCreated(result, "Subscription plan created successfully");
    }

    /// <summary>
    /// Updates an existing subscription plan.
    /// </summary>
    /// <param name="id">The subscription plan ID.</param>
    /// <param name="request">The subscription plan update request.</param>
    /// <returns>The updated subscription plan.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        var command = new UpdatePlan.UpdateSubscriptionPlanCommand(id, request);
        var result = await _mediator.Send(command);
        return ApiOk(result, "Subscription plan updated successfully");
    }
}
