using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Common.Interfaces;

public interface IPdfService
{
    byte[] GenerateDeckPdf(DeckDetailResponse deck);
    string BuildSafeFileName(string name);
}
