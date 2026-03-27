using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.SubscriptionPlans.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.SubscriptionPlans.Commands.CreateSubscriptionPlan;

public record CreateSubscriptionPlanCommand(CreateSubscriptionPlanRequest Request) : IRequest<SubscriptionPlanResponse>;

public class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, SubscriptionPlanResponse>
{
    private readonly IRepository<Guid, SubscriptionPlan> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSubscriptionPlanCommandHandler(
        IRepository<Guid, SubscriptionPlan> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SubscriptionPlanResponse> Handle(CreateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var orderExists = await _repository.GetQueryable()
            .AnyAsync(p => p.Order == command.Request.Order, cancellationToken);

        if (orderExists)
        {
            throw new ConflictException($"Subscription plan order {command.Request.Order} already exists.");
        }

        var plan = new SubscriptionPlan
        {
            Name = command.Request.Name,
            Description = command.Request.Description,
            DailyGenerateLimit = command.Request.DailyGenerateLimit,
            MaxQuestionsPerGenerate = command.Request.MaxQuestionsPerGenerate,
            HasExportToPdf = command.Request.HasExportToPdf,
            Price = command.Request.Price,
            Duration = command.Request.Duration,
            IsActive = command.Request.IsActive,
            Order = command.Request.Order
        };

        await _repository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubscriptionPlanResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            DailyGenerateLimit = plan.DailyGenerateLimit,
            MaxQuestionsPerGenerate = plan.MaxQuestionsPerGenerate,
            HasExportToPdf = plan.HasExportToPdf,
            Price = plan.Price,
            Duration = plan.Duration,
            IsActive = plan.IsActive,
            Order = plan.Order,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }
}
