namespace MyQuizGenerator.Application.SubscriptionPlans.DTOs;

public class UpdateSubscriptionPlanRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? DailyGenerateLimit { get; set; }
    public int? NumDeckLimit { get; set; }
    public decimal? Price { get; set; }
    public int? Duration { get; set; }
    public bool? IsActive { get; set; }
    public int? Order { get; set; }
}
