using MediatR;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Payments.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Payments.Commands.CreatePaymentOrder;

public record CreatePaymentOrderCommand(string UserId, CreatePaymentOrderRequest Request) : IRequest<PaymentOrderResponse>;

public class CreatePaymentOrderCommandHandler : IRequestHandler<CreatePaymentOrderCommand, PaymentOrderResponse>
{
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IRepository<Guid, PaymentTransaction> _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePaymentOrderCommandHandler> _logger;

    public CreatePaymentOrderCommandHandler(
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IRepository<Guid, PaymentTransaction> paymentRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreatePaymentOrderCommandHandler> logger)
    {
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentOrderResponse> Handle(CreatePaymentOrderCommand command, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionPlanRepository.GetActivePlanByIdAsync(command.Request.SubscriptionPlanId, cancellationToken);

        if (plan == null)
        {
            throw new NotFoundException("Subscription plan not found or is not active.");
        }

        if (plan.Price <= 0)
        {
            throw new BadRequestException("This plan is free and does not require payment.");
        }

        // Generate unique order code: MQSUB + timestamp + random
        var orderCode = $"MQSUB{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

        var transaction = new PaymentTransaction
        {
            UserId = command.UserId,
            SubscriptionPlanId = plan.Id,
            OrderCode = orderCode,
            Amount = plan.Price,
            Status = PaymentStatus.Pending
        };

        await _paymentRepository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment order created: {OrderCode} for user {UserId}, plan {PlanName}",
            orderCode, command.UserId, plan.Name);

        return new PaymentOrderResponse
        {
            PaymentTransactionId = transaction.Id,
            OrderCode = orderCode,
            Amount = plan.Price,
            TransferContent = orderCode,
            PlanName = plan.Name,
            CreatedAt = transaction.CreatedAt
        };
    }

}
