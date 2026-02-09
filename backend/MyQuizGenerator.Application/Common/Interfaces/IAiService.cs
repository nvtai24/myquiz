using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IAiService
{
    Task<GeneratedDeckResponse> GenerateDeckAsync(string prompt, CancellationToken cancellationToken);
}
