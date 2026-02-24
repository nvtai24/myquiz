using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Infrastructure.Persistence;
using MyQuizGenerator.Infrastructure.Repositories;

namespace MyQuizGenerator.Infrastructure.Persistence.Repositories;

public class SubscriptionPlanRepository : Repository<Guid, SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SubscriptionPlan?> GetActivePlanByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, cancellationToken);
    }
}
