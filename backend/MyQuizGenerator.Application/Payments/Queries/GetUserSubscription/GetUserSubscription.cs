using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Payments.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Payments.Queries.GetUserSubscription;

public record GetUserSubscriptionQuery(string UserId) : IRequest<UserSubscriptionResponse?>;

public class GetUserSubscriptionQueryHandler : IRequestHandler<GetUserSubscriptionQuery, UserSubscriptionResponse?>
{
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;

    public GetUserSubscriptionQueryHandler(IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository)
    {
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    public async Task<UserSubscriptionResponse?> Handle(GetUserSubscriptionQuery query, CancellationToken cancellationToken)
    {
        var subscription = await _userSubscriptionRepository.GetQueryable()
            .Include(usp => usp.SubscriptionPlan)
            .Where(usp => usp.UserId == query.UserId)
            .OrderByDescending(usp => usp.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            return null;
        }

        return new UserSubscriptionResponse
        {
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            PlanName = subscription.SubscriptionPlan.Name,
            PlanDescription = subscription.SubscriptionPlan.Description,
            DailyGenerateLimit = subscription.SubscriptionPlan.DailyGenerateLimit,
            NumDeckLimit = subscription.SubscriptionPlan.NumDeckLimit,
            Price = subscription.SubscriptionPlan.Price,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            IsExpired = subscription.EndDate < DateTime.UtcNow
        };
    }
}
