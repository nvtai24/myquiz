namespace MyQuizGenerator.Application.Decks.DTOs;

public class QuizAttemptHistoryResponse
{
    public Guid AttemptId { get; set; }
    public Guid DeckId { get; set; }
    public string DeckName { get; set; } = string.Empty;
    public string DeckThumbnailUrl { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double ScorePercent { get; set; }
    public int TotalTime { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
}
