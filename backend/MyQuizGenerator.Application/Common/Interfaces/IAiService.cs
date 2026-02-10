using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IAiService
{
    /// <summary>
    /// Generate full deck (name, description, tags, questions) from first chunk
    /// </summary>
    Task<GeneratedDeckResponse> GenerateDeckAsync(string text, CancellationToken cancellationToken);

    /// <summary>
    /// Generate only questions from subsequent chunks
    /// </summary>
    Task<List<GeneratedQuestionResponse>> GenerateQuestionsFromChunkAsync(string chunkText, int chunkIndex, CancellationToken cancellationToken);
}
