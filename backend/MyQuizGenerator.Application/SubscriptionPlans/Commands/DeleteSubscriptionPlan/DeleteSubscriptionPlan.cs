using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.SubscriptionPlans.Commands.DeleteSubscriptionPlan;

public record DeleteSubscriptionPlanCommand(Guid Id) : IRequest;

public class DeleteSubscriptionPlanCommandHandler : IRequestHandler<DeleteSubscriptionPlanCommand>
{
    private readonly IRepository<Guid, SubscriptionPlan> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSubscriptionPlanCommandHandler(
        IRepository<Guid, SubscriptionPlan> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SubscriptionPlan), command.Id);

        _repository.Delete(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
