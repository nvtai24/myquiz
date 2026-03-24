namespace MyQuizGenerator.Application.QuizAttempts.DTOs;

public class CreateQuizAttemptRequest
{
    public Guid DeckId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int TotalTime { get; set; }
    public List<UserAnswerRequest> UserAnswers { get; set; } = new();
}

public class UserAnswerRequest
{
    public int QuestionId { get; set; }
    public string QuestionSnapshot { get; set; } = string.Empty;
    public string[] OptionsSnapshot { get; set; } = [];
    public string[] CorrectAnswersSnapshot { get; set; } = [];
    public string[] Answer { get; set; } = [];
    public bool IsCorrect { get; set; }
}
