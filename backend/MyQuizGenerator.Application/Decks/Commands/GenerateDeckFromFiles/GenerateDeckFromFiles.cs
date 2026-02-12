using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Decks.DTOs;

namespace MyQuizGenerator.Application.Decks.Commands.GenerateDeckFromFiles;

public record GenerateDeckFromFilesCommand(Stream FileStream, string FileName, string ContentType) : IRequest<GeneratedDeckResponse>;

public class GenerateDeckFromFilesCommandHandler : IRequestHandler<GenerateDeckFromFilesCommand, GeneratedDeckResponse>
{
    private readonly IDocumentService _documentService;
    private readonly IAiService _aiService;

    public GenerateDeckFromFilesCommandHandler(
        IDocumentService documentService,
        IAiService aiService)
    {
        _documentService = documentService;
        _aiService = aiService;
    }

    private const int ChunkSize = 15000; // Characters per chunk

    public async Task<GeneratedDeckResponse> Handle(GenerateDeckFromFilesCommand request, CancellationToken cancellationToken)
    {
        // 1. Extract text from the file
        using var memoryStream = new MemoryStream();
        await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var text = await _documentService.ExtractTextAsync(memoryStream, request.FileName, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new Exception("Could not extract text from file.");
        }

        // 2. Chunk text and generate questions
        var chunks = ChunkText(text, ChunkSize);
        GeneratedDeckResponse generatedDeck;

        if (chunks.Count == 1)
        {
            generatedDeck = await _aiService.GenerateDeckAsync(chunks[0], cancellationToken);
        }
        else
        {
            generatedDeck = await _aiService.GenerateDeckAsync(chunks[0], cancellationToken);

            for (int i = 1; i < chunks.Count; i++)
            {
                var additionalQuestions = await _aiService.GenerateQuestionsFromChunkAsync(chunks[i], i, cancellationToken);
                generatedDeck.Questions.AddRange(additionalQuestions);
            }
        }

        // 3. Return generated deck (no DB save - FE will call save API separately)
        return generatedDeck;
    }

    private static List<string> ChunkText(string text, int chunkSize)
    {
        var chunks = new List<string>();

        if (string.IsNullOrEmpty(text))
            return chunks;

        // Try to split at paragraph boundaries for better context
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            // If adding this paragraph exceeds chunk size, save current chunk and start new one
            if (currentChunk.Length + paragraph.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }

            // If single paragraph is larger than chunk size, split it
            if (paragraph.Length > chunkSize)
            {
                // Save any pending content first
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // Split large paragraph by sentences or fixed size
                for (int i = 0; i < paragraph.Length; i += chunkSize)
                {
                    var length = Math.Min(chunkSize, paragraph.Length - i);
                    chunks.Add(paragraph.Substring(i, length).Trim());
                }
            }
            else
            {
                currentChunk.AppendLine(paragraph);
            }
        }

        // Add remaining content
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}
