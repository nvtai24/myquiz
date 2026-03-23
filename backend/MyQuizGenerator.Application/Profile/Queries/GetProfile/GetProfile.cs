using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Profile.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Profile.Queries.GetProfile;

public record GetProfileQuery(string UserId) : IRequest<ProfileResponse>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    private readonly IAuthService _authService;
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;

    public GetProfileQueryHandler(
        IAuthService authService,
        IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository)
    {
        _authService = authService;
        _userSubscriptionRepository = userSubscriptionRepository;
    }

    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userInfo = await _authService.GetUserByIdAsync(request.UserId);

        if (userInfo == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        // Get current subscription plan name (EndDate > now, highest Order)
        var currentPlanName = await _userSubscriptionRepository.GetQueryable()
            .Include(usp => usp.SubscriptionPlan)
            .Where(usp => usp.UserId == request.UserId && usp.EndDate > DateTime.UtcNow)
            .OrderByDescending(usp => usp.SubscriptionPlan.Order)
            .Select(usp => usp.SubscriptionPlan.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return new ProfileResponse
        {
            Id = userInfo.Id,
            Email = userInfo.Email,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            AvatarUrl = userInfo.AvatarUrl,
            Roles = userInfo.Roles,
            CurrentPlan = currentPlanName
        };
    }
}
