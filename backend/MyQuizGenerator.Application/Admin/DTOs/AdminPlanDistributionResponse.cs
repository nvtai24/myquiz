namespace MyQuizGenerator.Application.Admin.DTOs;

public class AdminPlanDistributionResponse
{
    public string PlanName { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public double Percentage { get; set; }
}
