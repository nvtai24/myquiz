using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Domain.Enums;
using MyQuizGenerator.Infrastructure.Persistence;

namespace MyQuizGenerator.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that periodically marks expired pending payment transactions as Expired.
/// Runs every minute.
/// </summary>
public class ExpirePaymentTransactionsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirePaymentTransactionsJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public ExpirePaymentTransactionsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpirePaymentTransactionsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpirePaymentTransactionsJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpirePendingTransactionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring payment transactions.");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("ExpirePaymentTransactionsJob stopped.");
    }

    private async Task ExpirePendingTransactionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var expiredCount = await dbContext.Set<Domain.Entities.PaymentTransaction>()
            .Where(t => t.Status == PaymentStatus.Pending && t.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, PaymentStatus.Expired),
                cancellationToken);

        if (expiredCount > 0)
        {
            _logger.LogInformation("Marked {Count} payment transaction(s) as expired.", expiredCount);
        }
    }
}
