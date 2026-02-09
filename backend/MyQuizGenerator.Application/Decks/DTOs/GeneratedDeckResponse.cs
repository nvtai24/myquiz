namespace MyQuizGenerator.Application.Decks.DTOs;

public class GeneratedDeckResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string FileUrl { get; set; } = string.Empty;
    public List<GeneratedQuestionResponse> Questions { get; set; } = new();
}


