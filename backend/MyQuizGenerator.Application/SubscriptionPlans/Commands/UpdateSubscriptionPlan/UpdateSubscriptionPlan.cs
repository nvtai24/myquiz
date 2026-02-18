using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.SubscriptionPlans.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.SubscriptionPlans.Commands.UpdateSubscriptionPlan;

public record UpdateSubscriptionPlanCommand(Guid Id, UpdateSubscriptionPlanRequest Request) : IRequest<SubscriptionPlanResponse>;

public class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand, SubscriptionPlanResponse>
{
    private readonly IRepository<Guid, SubscriptionPlan> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSubscriptionPlanCommandHandler(
        IRepository<Guid, SubscriptionPlan> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SubscriptionPlanResponse> Handle(UpdateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SubscriptionPlan), command.Id);

        if (command.Request.Name is not null) plan.Name = command.Request.Name;
        if (command.Request.Description is not null) plan.Description = command.Request.Description;
        if (command.Request.DailyGenerateLimit.HasValue) plan.DailyGenerateLimit = command.Request.DailyGenerateLimit.Value;
        if (command.Request.NumDeckLimit.HasValue) plan.NumDeckLimit = command.Request.NumDeckLimit.Value;
        if (command.Request.Price.HasValue) plan.Price = command.Request.Price.Value;
        if (command.Request.Duration.HasValue) plan.Duration = command.Request.Duration.Value;
        if (command.Request.IsActive.HasValue) plan.IsActive = command.Request.IsActive.Value;
        if (command.Request.Order.HasValue) plan.Order = command.Request.Order.Value;

        plan.UpdatedAt = DateTime.UtcNow;

        _repository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubscriptionPlanResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            DailyGenerateLimit = plan.DailyGenerateLimit,
            NumDeckLimit = plan.NumDeckLimit,
            Price = plan.Price,
            Duration = plan.Duration,
            IsActive = plan.IsActive,
            Order = plan.Order,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }
}
