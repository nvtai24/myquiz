using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Queries.GetUserQuota;

public record GetUserQuotaQuery : IRequest<UserQuotaResponse>;

public class GetUserQuotaQueryHandler : IRequestHandler<GetUserQuotaQuery, UserQuotaResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;

    public GetUserQuotaQueryHandler(
        ICurrentUserService currentUserService,
        IRateLimitService rateLimitService,
        IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository)
    {
        _currentUserService = currentUserService;
        _rateLimitService = rateLimitService;
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    public async Task<UserQuotaResponse> Handle(GetUserQuotaQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? string.Empty;

        // Get user's active subscription plan with highest priority
        var now = DateTime.UtcNow;
        var activePlan = await _userSubscriptionRepository.GetQueryable()
            .Include(usp => usp.SubscriptionPlan)
            .Where(usp => usp.UserId == userId && usp.StartDate <= now && usp.EndDate > now)
            .OrderByDescending(usp => usp.SubscriptionPlan.Order)
            .Select(usp => usp.SubscriptionPlan)
            .FirstOrDefaultAsync(cancellationToken);

        // Get daily generate usage from Redis
        var dailyUsed = (int)await _rateLimitService.GetDailyGenerateCountAsync(userId, cancellationToken);

        var dailyLimit = activePlan?.DailyGenerateLimit ?? 0;

        return new UserQuotaResponse
        {
            PlanName = activePlan?.Name ?? "Free",
            DailyGenerateLimit = dailyLimit,
            DailyGenerateUsed = dailyUsed,
            // -1 = unlimited, otherwise calculate remaining
            DailyGenerateRemaining = dailyLimit < 0 ? -1 : Math.Max(0, dailyLimit - dailyUsed)
        };
    }
}
