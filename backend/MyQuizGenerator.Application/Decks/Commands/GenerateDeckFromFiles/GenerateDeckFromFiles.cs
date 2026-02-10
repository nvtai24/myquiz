using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Commands.GenerateDeckFromFiles;

public record GenerateDeckFromFilesCommand(Stream FileStream, string FileName, string ContentType) : IRequest<GeneratedDeckResponse>;

public class GenerateDeckFromFilesCommandHandler : IRequestHandler<GenerateDeckFromFilesCommand, GeneratedDeckResponse>
{
    private readonly IDocumentService _documentService;
    private readonly IAiService _aiService;
    private readonly IFileService _fileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public GenerateDeckFromFilesCommandHandler(
        IDocumentService documentService,
        IAiService aiService,
        IFileService fileService,
        IUnitOfWork unitOfWork,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _documentService = documentService;
        _aiService = aiService;
        _fileService = fileService;
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    private const int ChunkSize = 15000; // Characters per chunk

    public async Task<GeneratedDeckResponse> Handle(GenerateDeckFromFilesCommand request, CancellationToken cancellationToken)
    {
        // 0. Upload file
        var fileUploadRequest = new Application.Files.DTOs.FileUploadRequest(request.FileStream, request.FileName, request.ContentType);

        // We need a seekable stream for multiple reads (upload + extract)
        using var memoryStream = new MemoryStream();
        await request.FileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        // Re-create request with memory stream
        fileUploadRequest = fileUploadRequest with { FileStream = memoryStream };
        var fileUrl = await _fileService.UploadFileAsync(fileUploadRequest);

        // Reset for texts extraction
        memoryStream.Position = 0;

        // 1. Extract text
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
            // Single chunk - use original method
            generatedDeck = await _aiService.GenerateDeckAsync(chunks[0], cancellationToken);
        }
        else
        {
            // Multiple chunks - generate deck from first chunk, then questions from remaining chunks
            generatedDeck = await _aiService.GenerateDeckAsync(chunks[0], cancellationToken);

            // Process remaining chunks sequentially as requested for better reliability
            for (int i = 1; i < chunks.Count; i++)
            {
                var additionalQuestions = await _aiService.GenerateQuestionsFromChunkAsync(chunks[i], i, cancellationToken);
                generatedDeck.Questions.AddRange(additionalQuestions);
            }
        }

        // 3. Create Entities
        var deckId = Guid.NewGuid();
        var questionEntities = generatedDeck.Questions.Select(q => new Question
        {
            Content = q.Content,
            Type = q.Type,
            Hint = q.Hint,
            Explanation = q.Explanation,
            Options = q.Options,
            CorrectAnswers = q.CorrectAnswers,
            DeckId = deckId
        }).ToList();

        var uploadedFile = new UploadedFile
        {
            FileName = request.FileName,
            OriginalFileName = request.FileName,
            Url = fileUrl,
            ContentType = "application/octet-stream",
            Size = memoryStream.Length,
            DeckId = deckId,
            CreatedBy = _currentUserService.UserId
        };

        var deck = new Deck
        {
            Id = deckId,
            Name = generatedDeck.Name,
            Description = generatedDeck.Description,
            Visibility = DeckVisibility.Private, // Default private for drafts
            Status = DeckStatus.Draft,
            Source = DeckSource.AiGenerated,
            Tags = generatedDeck.Tags.ToArray(),
            OwnerId = _currentUserService.UserId ?? string.Empty,
            Questions = questionEntities,
            Documents = new List<UploadedFile> { uploadedFile }
        };

        // 4. Save to DB
        await _deckRepository.AddAsync(deck, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Update response with ID and FileUrl
        generatedDeck.Id = deck.Id;
        generatedDeck.FileUrl = fileUrl;

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
