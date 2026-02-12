namespace MyQuizGenerator.Domain.Entities;

public class UserAnswer
{
    public Guid Id { get; set; }
    public Guid QuizAttemptId { get; set; }
    public virtual QuizAttempt QuizAttempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public virtual Question Question { get; set; } = null!;
    public string[] Answer { get; set; } = [];
    public bool IsCorrect { get; set; }
}