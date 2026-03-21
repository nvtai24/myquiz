using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Admin.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Admin.Queries.GetStatsSummary;

public record GetStatsSummaryQuery() : IRequest<AdminStatsSummaryResponse>;

public class GetStatsSummaryQueryHandler : IRequestHandler<GetStatsSummaryQuery, AdminStatsSummaryResponse>
{
    private readonly IRepository<Guid, PaymentTransaction> _paymentRepository;
    private readonly IUserService _userService;

    public GetStatsSummaryQueryHandler(
        IRepository<Guid, PaymentTransaction> paymentRepository,
        IUserService userService)
    {
        _paymentRepository = paymentRepository;
        _userService = userService;
    }

    public async Task<AdminStatsSummaryResponse> Handle(GetStatsSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = currentMonthStart.AddMonths(-1);
        var lastMonthEnd = currentMonthStart.AddSeconds(-1);

        // Revenue calculations
        var allCompletedPayments = await _paymentRepository.GetQueryable()
            .Where(p => p.Status == PaymentStatus.Completed)
            .Select(p => new { p.Amount, p.CreatedAt })
            .ToListAsync(cancellationToken);

        var totalRevenue = allCompletedPayments.Sum(p => p.Amount);
        
        var currentMonthRevenue = allCompletedPayments
            .Where(p => p.CreatedAt >= currentMonthStart && p.CreatedAt <= now)
            .Sum(p => p.Amount);

        var lastMonthRevenue = allCompletedPayments
            .Where(p => p.CreatedAt >= lastMonthStart && p.CreatedAt <= lastMonthEnd)
            .Sum(p => p.Amount);

        double revenueGrowth = 0;
        if (lastMonthRevenue > 0)
        {
            revenueGrowth = (double)((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
        }
        else if (currentMonthRevenue > 0)
        {
            revenueGrowth = 100;
        }

        revenueGrowth = Math.Round(revenueGrowth, 1);

        // Users calculations
        var currentMonthUsers = await _userService.GetNewUsersCountAsync(currentMonthStart, now);
        var lastMonthUsers = await _userService.GetNewUsersCountAsync(lastMonthStart, lastMonthEnd);

        double userGrowth = 0;
        if (lastMonthUsers > 0)
        {
            userGrowth = ((double)(currentMonthUsers - lastMonthUsers) / lastMonthUsers) * 100;
        }
        else if (currentMonthUsers > 0)
        {
            userGrowth = 100;
        }

        userGrowth = Math.Round(userGrowth, 1);

        return new AdminStatsSummaryResponse
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = currentMonthRevenue,
            RevenueGrowth = revenueGrowth,
            NewUsers = currentMonthUsers,
            UserGrowth = userGrowth
        };
    }
}
