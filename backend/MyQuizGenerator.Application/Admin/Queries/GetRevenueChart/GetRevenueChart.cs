using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Admin.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Admin.Queries.GetRevenueChart;

public record GetRevenueChartQuery(int Days = 7) : IRequest<List<AdminRevenueChartResponse>>;

public class GetRevenueChartQueryHandler : IRequestHandler<GetRevenueChartQuery, List<AdminRevenueChartResponse>>
{
    private readonly IRepository<Guid, PaymentTransaction> _paymentRepository;

    public GetRevenueChartQueryHandler(IRepository<Guid, PaymentTransaction> paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<List<AdminRevenueChartResponse>> Handle(GetRevenueChartQuery request, CancellationToken cancellationToken)
    {
        var days = request.Days > 0 ? request.Days : 7;
        
        // We want to include today, so startDate is (days - 1) days ago.
        var today = DateTime.UtcNow.Date;
        var startDate = today.AddDays(-(days - 1));

        var transactions = await _paymentRepository.GetQueryable()
            .Where(p => p.Status == PaymentStatus.Completed && p.CreatedAt >= startDate)
            .Select(p => new { p.Amount, p.CreatedAt })
            .ToListAsync(cancellationToken);

        // Group by Date in memory
        var groupedData = transactions
            .GroupBy(p => p.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var result = new List<AdminRevenueChartResponse>();

        // Generate exactly 'days' elements, including days with 0 amount
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var amount = groupedData.ContainsKey(date) ? groupedData[date] : 0;
            
            result.Add(new AdminRevenueChartResponse
            {
                Date = date.ToString("dd/MM"),
                Amount = amount
            });
        }

        return result;
    }
}
