using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.SubscriptionPlans.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.SubscriptionPlans.Queries.GetActiveSubscriptionPlans;

public record GetActiveSubscriptionPlansQuery() : IRequest<List<SubscriptionPlanSummaryResponse>>;

public class GetActiveSubscriptionPlansQueryHandler : IRequestHandler<GetActiveSubscriptionPlansQuery, List<SubscriptionPlanSummaryResponse>>
{
    private readonly IRepository<Guid, SubscriptionPlan> _repository;

    public GetActiveSubscriptionPlansQueryHandler(IRepository<Guid, SubscriptionPlan> repository)
    {
        _repository = repository;
    }

    public async Task<List<SubscriptionPlanSummaryResponse>> Handle(GetActiveSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _repository.GetAllAsync(cancellationToken);

        return plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Order)
            .Select(p => new SubscriptionPlanSummaryResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DailyGenerateLimit = p.DailyGenerateLimit,
                MaxQuestionsPerGenerate = p.MaxQuestionsPerGenerate,
                HasExportToPdf = p.HasExportToPdf,
                Price = p.Price,
                Duration = p.Duration,
                Order = p.Order
            }).ToList();
    }
}
