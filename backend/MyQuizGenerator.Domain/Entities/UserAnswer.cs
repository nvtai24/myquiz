using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Domain.Entities;

public class UserAnswer
{
    public Guid Id { get; set; }
    public Guid QuizAttemptId { get; set; }
    public virtual QuizAttempt QuizAttempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public virtual Question Question { get; set; } = null!;
    public string QuestionSnapshot { get; set; } = string.Empty;
    public QuestionType TypeSnapshot { get; set; }
    public string HintSnapshot { get; set; } = string.Empty;
    public string ExplanationSnapshot { get; set; } = string.Empty;
    public string[] OptionsSnapshot { get; set; } = [];
    public string[] CorrectAnswersSnapshot { get; set; } = [];
    public string[] Answer { get; set; } = [];
    public bool IsCorrect { get; set; }
}