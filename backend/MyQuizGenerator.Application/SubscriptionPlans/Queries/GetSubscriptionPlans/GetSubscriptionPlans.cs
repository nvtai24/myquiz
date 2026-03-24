using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.SubscriptionPlans.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.SubscriptionPlans.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery() : IRequest<List<SubscriptionPlanResponse>>;

public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, List<SubscriptionPlanResponse>>
{
    private readonly IRepository<Guid, SubscriptionPlan> _repository;

    public GetSubscriptionPlansQueryHandler(IRepository<Guid, SubscriptionPlan> repository)
    {
        _repository = repository;
    }

    public async Task<List<SubscriptionPlanResponse>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _repository.GetAllAsync(cancellationToken);

        return plans.OrderBy(p => p.Order).Select(p => new SubscriptionPlanResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            DailyGenerateLimit = p.DailyGenerateLimit,
            Price = p.Price,
            Duration = p.Duration,
            IsActive = p.IsActive,
            Order = p.Order,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }
}
