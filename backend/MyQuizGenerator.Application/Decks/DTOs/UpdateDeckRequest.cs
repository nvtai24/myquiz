using System.ComponentModel.DataAnnotations;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.DTOs;

public class UpdateDeckRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DeckVisibility Visibility { get; set; } = DeckVisibility.Public;

    public string[] Tags { get; set; } = Array.Empty<string>();

    // Thumbnail file info (populated by Controller from IFormFile)
    public Stream? ThumbnailStream { get; set; }
    public string? ThumbnailFileName { get; set; }
    public string? ThumbnailContentType { get; set; }
}
