using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Admin.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Admin.Queries.GetPlanDistribution;

public record GetPlanDistributionQuery() : IRequest<List<AdminPlanDistributionResponse>>;

public class GetPlanDistributionQueryHandler : IRequestHandler<GetPlanDistributionQuery, List<AdminPlanDistributionResponse>>
{
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;

    public GetPlanDistributionQueryHandler(IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository)
    {
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    public async Task<List<AdminPlanDistributionResponse>> Handle(GetPlanDistributionQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var activeSubs = await _userSubscriptionRepository.GetQueryable()
            .Include(usp => usp.SubscriptionPlan)
            .Where(usp => usp.EndDate >= now)
            .Select(usp => new { usp.UserId, PlanName = usp.SubscriptionPlan.Name })
            .ToListAsync(cancellationToken);

        var distribution = activeSubs
            .GroupBy(s => s.PlanName)
            .Select(g => new 
            { 
                PlanName = g.Key, 
                UserCount = g.Select(x => x.UserId).Distinct().Count() 
            })
            .OrderByDescending(x => x.UserCount)
            .ToList();

        var totalUsers = distribution.Sum(d => d.UserCount);
        var result = new List<AdminPlanDistributionResponse>();

        foreach (var item in distribution)
        {
            var percentage = totalUsers > 0 ? Math.Round((double)item.UserCount / totalUsers * 100, 1) : 0;
            
            result.Add(new AdminPlanDistributionResponse
            {
                PlanName = item.PlanName,
                UserCount = item.UserCount,
                Percentage = percentage
            });
        }

        return result;
    }
}
