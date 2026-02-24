namespace MyQuizGenerator.Application.Payments.DTOs;

public class PaymentOrderResponse
{
    public Guid PaymentTransactionId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
