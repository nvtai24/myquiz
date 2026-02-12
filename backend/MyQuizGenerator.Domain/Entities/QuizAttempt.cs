namespace MyQuizGenerator.Domain.Entities;

public class QuizAttempt
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public virtual Deck Deck { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int TotalTime { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}