using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Admin.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Admin.Queries.GetPaymentTransactions;

public record GetPaymentTransactionsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    PaymentStatus? Status = null)
    : IRequest<(List<AdminPaymentTransactionResponse> Transactions, int TotalCount)>;

public class GetPaymentTransactionsQueryHandler
    : IRequestHandler<GetPaymentTransactionsQuery, (List<AdminPaymentTransactionResponse> Transactions, int TotalCount)>
{
    private readonly IRepository<Guid, PaymentTransaction> _paymentRepository;
    private readonly IUserService _userService;

    public GetPaymentTransactionsQueryHandler(
        IRepository<Guid, PaymentTransaction> paymentRepository,
        IUserService userService)
    {
        _paymentRepository = paymentRepository;
        _userService = userService;
    }

    public async Task<(List<AdminPaymentTransactionResponse> Transactions, int TotalCount)> Handle(
        GetPaymentTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _paymentRepository.GetQueryable()
            .Include(p => p.SubscriptionPlan)
            .AsQueryable();

        // Filter by status
        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        // Search by order code or content
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(p =>
                p.OrderCode.ToLower().Contains(searchLower) ||
                p.Content.ToLower().Contains(searchLower) ||
                p.UserId.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = new List<AdminPaymentTransactionResponse>();
        foreach (var tx in transactions)
        {
            var userInfo = await _userService.GetUserInfoAsync(tx.UserId);
            result.Add(new AdminPaymentTransactionResponse
            {
                Id = tx.Id,
                UserId = tx.UserId,
                UserEmail = userInfo?.Email ?? string.Empty,
                UserFullName = userInfo?.FullName ?? string.Empty,
                PlanName = tx.SubscriptionPlan?.Name ?? string.Empty,
                OrderCode = tx.OrderCode,
                Amount = tx.Amount,
                Status = tx.Status,
                Content = tx.Content,
                CreatedAt = tx.CreatedAt,
                CompletedAt = tx.CompletedAt
            });
        }

        return (result, totalCount);
    }
}
