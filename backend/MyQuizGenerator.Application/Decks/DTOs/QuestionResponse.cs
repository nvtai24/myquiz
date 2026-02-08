using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.DTOs;

public class QuestionResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string Hint { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string[] Options { get; set; } = Array.Empty<string>();
    public string[] CorrectAnswers { get; set; } = Array.Empty<string>();
}
