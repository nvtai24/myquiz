using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.DTOs;

public class RecommendedDeckActivitySignalResponse
{
    public string[]? Tags { get; set; }
    public DateTime ActivityAt { get; set; }
    public double Weight { get; set; }
}

public class RecommendedDeckCandidateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeckVisibility Visibility { get; set; }
    public DeckStatus Status { get; set; }
    public string[]? Tags { get; set; }
    public int QuestionCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public int TotalRatings { get; set; }
    public double AverageRating { get; set; }
    public int SaveCount { get; set; }
    public int ViewCount { get; set; }
    public int AttemptCount { get; set; }
}

public class RankedRecommendedDeckResponse
{
    public RecommendedDeckCandidateResponse Deck { get; set; } = new();
    public double Score { get; set; }
    public double TagMatchScore { get; set; }
    public DateTime FreshestAt { get; set; }
}

public class HotTagDeckProjectionResponse
{
    public string[]? Tags { get; set; }
    public DateTime LastActiveAt { get; set; }
}

public class HotTagActivityResponse
{
    public string Tag { get; set; } = string.Empty;
    public DateTime LastActiveAt { get; set; }
}

public class HotTagAggregateResponse
{
    public string Tag { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LatestAt { get; set; }
}

public class TrendingDeckCandidateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeckVisibility Visibility { get; set; }
    public DeckStatus Status { get; set; }
    public string[]? Tags { get; set; }
    public int QuestionCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public int TotalRatings { get; set; }
    public double AverageRating { get; set; }
    public int ViewCount { get; set; }
    public int RecentViewCount { get; set; }
    public int SaveCount { get; set; }
    public int RecentSaveCount { get; set; }
    public int AttemptCount { get; set; }
    public int RecentAttemptCount { get; set; }
}

public class HotTagMomentumResponse
{
    public string Tag { get; set; } = string.Empty;
    public DateTime LastActiveAt { get; set; }
}

public class RankedTrendingDeckResponse
{
    public TrendingDeckCandidateResponse Deck { get; set; } = new();
    public double Score { get; set; }
    public DateTime FreshestAt { get; set; }
}
