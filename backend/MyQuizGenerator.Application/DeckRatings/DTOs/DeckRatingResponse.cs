namespace MyQuizGenerator.Application.DeckRatings.DTOs;

public class DeckRatingResponse
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DeckRatingSummaryResponse
{
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public List<DeckRatingResponse> Ratings { get; set; } = new();
}
