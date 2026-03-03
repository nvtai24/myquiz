namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IRateLimitService
{
    /// <summary>
    /// Tăng counter gen quiz của user hôm nay. Trả về số lần đã gen sau khi tăng.
    /// </summary>
    Task<long> IncrementDailyGenerateCountAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Lấy số lần đã gen hôm nay (không tăng).
    /// </summary>
    Task<long> GetDailyGenerateCountAsync(string userId, CancellationToken ct = default);
}
