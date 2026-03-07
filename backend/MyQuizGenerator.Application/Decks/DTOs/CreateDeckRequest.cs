using System.ComponentModel.DataAnnotations;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.DTOs;

public class CreateDeckRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DeckVisibility Visibility { get; set; } = DeckVisibility.Public;

    public DeckStatus Status { get; set; } = DeckStatus.Draft;

    public DeckSource Source { get; set; } = DeckSource.AiGenerated;

    public string[] Tags { get; set; } = Array.Empty<string>();

    public List<CreateQuestionRequest> Questions { get; set; } = new();

    // File info (populated by Controller from IFormFile)
    public Stream? FileStream { get; set; }
    public string? FileName { get; set; }
    public string? FileContentType { get; set; }

    // Thumbnail file info (populated by Controller from IFormFile)
    public Stream? ThumbnailStream { get; set; }
    public string? ThumbnailFileName { get; set; }
    public string? ThumbnailContentType { get; set; }
}

