using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Common.Interfaces.Repositories;

public interface ISubscriptionPlanRepository : IRepository<Guid, SubscriptionPlan>
{
    Task<SubscriptionPlan?> GetActivePlanByIdAsync(Guid planId, CancellationToken cancellationToken = default);
}
