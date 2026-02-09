namespace MyQuizGenerator.Application.Decks.DTOs;

public class DeckDocumentResponse
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
}