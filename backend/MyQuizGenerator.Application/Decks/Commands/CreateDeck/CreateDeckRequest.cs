using System.ComponentModel.DataAnnotations;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Commands.CreateDeck;

public class CreateDeckRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DeckVisibility Visibility { get; set; } = DeckVisibility.Public;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public List<CreateQuestionRequest> Questions { get; set; } = new();
}

public class CreateQuestionRequest
{
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;

    public string Hint { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;

    public string[] Options { get; set; } = Array.Empty<string>();

    public string[] CorrectAnswers { get; set; } = Array.Empty<string>();
}
