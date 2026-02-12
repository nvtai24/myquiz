using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Domain.Entities;

public class Deck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DeckVisibility Visibility { get; set; }
    public DeckStatus Status { get; set; }
    public DeckSource Source { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = null;
    public string OwnerId { get; set; } = string.Empty;
    public ICollection<UploadedFile> Documents { get; set; } = new List<UploadedFile>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<DeckInvitation> DeckInvitations { get; set; } = new List<DeckInvitation>();
    public ICollection<DeckMember> DeckMembers { get; set; } = new List<DeckMember>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}



