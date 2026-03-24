namespace MyQuizGenerator.Application.SubscriptionPlans.DTOs;

public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DailyGenerateLimit { get; set; }
    public int MaxQuestionsPerGenerate { get; set; }
    public bool HasExportToPdf { get; set; }
    public decimal Price { get; set; }
    public int Duration { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
