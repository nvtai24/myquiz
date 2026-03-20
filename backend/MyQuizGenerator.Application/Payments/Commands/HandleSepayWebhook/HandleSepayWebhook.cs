using System.Globalization;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Payments.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Payments.Commands.HandleSepayWebhook;

public record HandleSepayWebhookCommand(SepayWebhookRequest Request) : IRequest<bool>;

public class HandleSepayWebhookCommandHandler : IRequestHandler<HandleSepayWebhookCommand, bool>
{
    private readonly IRepository<Guid, PaymentTransaction> _paymentRepository;
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;
    private readonly IRepository<Guid, SubscriptionPlan> _subscriptionPlanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HandleSepayWebhookCommandHandler> _logger;

    public HandleSepayWebhookCommandHandler(
        IRepository<Guid, PaymentTransaction> paymentRepository,
        IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository,
        IRepository<Guid, SubscriptionPlan> subscriptionPlanRepository,
        IUnitOfWork unitOfWork,
        ILogger<HandleSepayWebhookCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(HandleSepayWebhookCommand command, CancellationToken cancellationToken)
    {
        var webhook = command.Request;

        _logger.LogInformation("Received Sepay webhook: TransferAmount={Amount}, Content={Content}, Gateway={Gateway}",
            webhook.TransferAmount, webhook.Content, webhook.Gateway);

        // Only process incoming transfers
        if (webhook.TransferType != "in")
        {
            _logger.LogInformation("Skipping non-incoming transfer: {TransferType}", webhook.TransferType);
            return true;
        }

        // Extract order code from content (format: MQSUB + timestamp + random)
        var orderCodeMatch = Regex.Match(webhook.Content, @"(MQSUB\d+)", RegexOptions.IgnoreCase);
        if (!orderCodeMatch.Success)
        {
            _logger.LogWarning("No order code found in webhook content: {Content}", webhook.Content);
            return true; // Return true to acknowledge webhook (don't retry)
        }

        var orderCode = orderCodeMatch.Groups[1].Value.ToUpper();

        // Find the pending payment transaction
        var transaction = await _paymentRepository.GetQueryable()
            .FirstOrDefaultAsync(t => t.OrderCode == orderCode && t.Status == PaymentStatus.Pending, cancellationToken);

        if (transaction == null)
        {
            _logger.LogWarning("No pending transaction found for order code: {OrderCode}", orderCode);
            return true;
        }

        // Check if transaction has expired
        if (DateTime.UtcNow > transaction.ExpiresAt)
        {
            transaction.Status = PaymentStatus.Expired;
            _paymentRepository.Update(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Transaction expired for order code: {OrderCode}, expired at {ExpiresAt}",
                orderCode, transaction.ExpiresAt);
            return true;
        }

        // Verify amount matches
        if (webhook.TransferAmount < transaction.Amount)
        {
            _logger.LogWarning("Amount mismatch for order {OrderCode}: expected {Expected}, received {Received}",
                orderCode, transaction.Amount, webhook.TransferAmount);
            return true;
        }

        // Update transaction with Sepay data
        transaction.Status = PaymentStatus.Completed;
        transaction.SepayTransactionId = webhook.Id;
        transaction.Gateway = webhook.Gateway;
        transaction.AccountNumber = webhook.AccountNumber;
        transaction.Content = webhook.Content;
        transaction.Description = webhook.Description;
        transaction.CompletedAt = DateTime.UtcNow;
        transaction.TransactionDate = DateTime.SpecifyKind(DateTime.Parse(webhook.TransactionDate), DateTimeKind.Utc);

        _paymentRepository.Update(transaction);

        // Get the subscription plan to determine duration
        var plan = await _subscriptionPlanRepository.GetByIdAsync(transaction.SubscriptionPlanId, cancellationToken);
        if (plan == null)
        {
            _logger.LogError("Subscription plan not found: {PlanId}", transaction.SubscriptionPlanId);
            return false;
        }

        // Find existing active subscription for this user
        // var existingSubscription = await _userSubscriptionRepository.GetQueryable()
        //     .Where(usp => usp.UserId == transaction.UserId)
        //     .OrderByDescending(usp => usp.EndDate)
        //     .FirstOrDefaultAsync(cancellationToken);

        // var startDate = DateTime.UtcNow;

        // If user has an active subscription, extend from its end date
        // if (existingSubscription != null && existingSubscription.EndDate > DateTime.UtcNow)
        // {
        //     startDate = existingSubscription.EndDate;
        // }

        var newSubscription = new UserSubscriptionPlan
        {
            UserId = transaction.UserId,
            SubscriptionPlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.Duration)
        };

        await _userSubscriptionRepository.AddAsync(newSubscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Subscription activated for user {UserId}, plan {PlanName}, order {OrderCode}",
            transaction.UserId, plan.Name, orderCode);

        return true;
    }
}
