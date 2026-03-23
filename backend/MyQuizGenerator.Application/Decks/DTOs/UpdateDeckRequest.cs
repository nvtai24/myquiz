using System.ComponentModel.DataAnnotations;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.DTOs;

public class UpdateDeckRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DeckStatus Status { get; set; } = DeckStatus.Draft;

    public DeckVisibility Visibility { get; set; } = DeckVisibility.Public;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public string? ThumbnailUrl { get; set; }

    // Questions management
    public List<CreateQuestionRequest> QuestionsToAdd { get; set; } = [];

    public List<UpdateQuestionRequest> QuestionsToUpdate { get; set; } = [];

    public List<int> QuestionIdsToDelete { get; set; } = [];
}
