using System.ComponentModel.DataAnnotations;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.DTOs;

public class CreateDeckRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DeckVisibility Visibility { get; set; } = DeckVisibility.Public;

    public DeckSource Source { get; set; } = DeckSource.AiGenerated;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public List<CreateQuestionRequest> Questions { get; set; } = new();
}

