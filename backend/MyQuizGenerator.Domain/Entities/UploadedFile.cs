using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Domain.Entities;

public class UploadedFile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FileName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    // public Guid? DeckId { get; set; }
}
