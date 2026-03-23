using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Domain.Entities;
public class Question
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string Hint { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string[] Options { get; set; } = Array.Empty<string>();
    public string[] CorrectAnswers { get; set; } = Array.Empty<string>();
    public Guid DeckId { get; set; }
    public virtual Deck Deck { get; set; } = null!;

    // Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Maybe not needed
    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}

