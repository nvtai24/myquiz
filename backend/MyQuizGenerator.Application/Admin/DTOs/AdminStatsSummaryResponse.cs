namespace MyQuizGenerator.Application.Admin.DTOs;

public class AdminStatsSummaryResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public double RevenueGrowth { get; set; }
    public int NewUsers { get; set; }
    public double UserGrowth { get; set; }
}
