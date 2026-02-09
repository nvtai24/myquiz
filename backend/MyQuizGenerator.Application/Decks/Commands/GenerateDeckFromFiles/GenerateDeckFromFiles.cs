using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Commands.GenerateDeckFromFiles;

public record GenerateDeckFromFilesCommand(Stream FileStream, string FileName) : IRequest<GeneratedDeckResponse>;

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

    public async Task<GeneratedDeckResponse> Handle(GenerateDeckFromFilesCommand request, CancellationToken cancellationToken)
    {
        // 0. Upload file
        var fileUploadRequest = new Application.Files.DTOs.FileUploadRequest(request.FileStream, request.FileName, "application/octet-stream");

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

        if (text.Length > 100000)
        {
            text = text.Substring(0, 100000);
        }

        // 2. Generate Deck
        var generatedDeck = await _aiService.GenerateDeckAsync(text, cancellationToken);

        // 3. Create Entities
        var deckId = Guid.NewGuid();
        var questions = generatedDeck.Questions.Select(q => new Question
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
            Questions = questions,
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
}
