namespace MyQuizGenerator.Application.Payments.DTOs;

public class UserSubscriptionResponse
{
    public Guid SubscriptionPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanDescription { get; set; } = string.Empty;
    public int DailyGenerateLimit { get; set; }
    public int NumDeckLimit { get; set; }
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsExpired { get; set; }
}
