using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Domain.Entities
{
    public class PaymentTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public Guid SubscriptionPlanId { get; set; }
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        public string OrderCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // SePay / Bank fields
        public long SepayTransactionId { get; set; }
        public string? Gateway { get; set; }
        public string? AccountNumber { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
