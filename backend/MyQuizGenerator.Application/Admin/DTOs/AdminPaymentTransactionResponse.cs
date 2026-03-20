using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Admin.DTOs;

/// <summary>
/// Payment transaction details DTO for admin view.
/// </summary>
public class AdminPaymentTransactionResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
