using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Commands.GenerateDeckFromFiles;

public record GenerateDeckFromFilesCommand(Stream FileStream, string FileName) : IRequest<GeneratedDeckResponse>;

public class GenerateDeckFromFilesCommandHandler : IRequestHandler<GenerateDeckFromFilesCommand, GeneratedDeckResponse>
{
    private readonly IDocumentService _documentService;
    private readonly IAiService _aiService;

    public GenerateDeckFromFilesCommandHandler(IDocumentService documentService, IAiService aiService)
    {
        _documentService = documentService;
        _aiService = aiService;
    }

    public async Task<GeneratedDeckResponse> Handle(GenerateDeckFromFilesCommand request, CancellationToken cancellationToken)
    {
        // 1. Extract text from file
        var text = await _documentService.ExtractTextAsync(request.FileStream, request.FileName, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            return new GeneratedDeckResponse();
        }

        // 2. Limit text length if necessary (OpenAI has token limits, but GPT-4o is large)
        // For now, let's truncate to ~100k chars to be safe/cost-effective if file is huge
        if (text.Length > 100000)
        {
            text = text.Substring(0, 100000);
        }

        // 3. Generate completed deck structure
        var deck = await _aiService.GenerateDeckAsync(text, cancellationToken);

        return deck;
    }
}
