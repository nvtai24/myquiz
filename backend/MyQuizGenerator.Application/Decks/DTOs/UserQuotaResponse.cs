namespace MyQuizGenerator.Application.Decks.DTOs;

public class UserQuotaResponse
{
    public string PlanName { get; set; } = "Free";
    public int DailyGenerateLimit { get; set; }
    public int DailyGenerateUsed { get; set; }
    public int DailyGenerateRemaining { get; set; }
    public int NumDeckLimit { get; set; }
    public int NumDeckUsed { get; set; }
    public int NumDeckRemaining { get; set; }
}
