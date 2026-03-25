using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.QuizAttempts.DTOs;

public class QuizAttemptResponse
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public string DeckName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int TotalTime { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double Score { get; set; }
    public List<UserAnswerResponse> UserAnswers { get; set; } = new();
}

public class UserAnswerResponse
{
    public Guid Id { get; set; }
    public int QuestionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string Hint { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string[] Options { get; set; } = [];
    public string[] CorrectAnswers { get; set; } = [];
    public string[] Answer { get; set; } = [];
    public bool IsCorrect { get; set; }
}
