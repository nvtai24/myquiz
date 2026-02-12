using MediatR;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Application.Files.DTOs;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.Decks.Commands.CreateDeck;

public record CreateDeckCommand(CreateDeckRequest Request) : IRequest<Guid>;

public class CreateDeckCommandHandler : IRequestHandler<CreateDeckCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileService _fileService;

    public CreateDeckCommandHandler(
        IUnitOfWork unitOfWork,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService,
        IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
        _fileService = fileService;
    }

    public async Task<Guid> Handle(CreateDeckCommand request, CancellationToken cancellationToken)
    {
        var listQuestions = new List<Question>();
        var deckId = Guid.NewGuid();

        foreach (var item in request.Request.Questions)
        {
            listQuestions.Add(new Question
            {
                Content = item.Content,
                Type = item.Type,
                Hint = item.Hint,
                Explanation = item.Explanation,
                Options = item.Options,
                CorrectAnswers = item.CorrectAnswers,
                DeckId = deckId
            });
        }

        var deck = new Deck
        {
            Id = deckId,
            Name = request.Request.Name,
            Description = request.Request.Description,
            Visibility = request.Request.Visibility,
            Status = request.Request.Status,
            Source = request.Request.Source,
            Tags = request.Request.Tags,
            OwnerId = _currentUserService.UserId ?? string.Empty,
            Questions = listQuestions
        };

        // Upload file and attach to deck if provided
        if (request.Request.FileStream != null && !string.IsNullOrEmpty(request.Request.FileName))
        {
            var fileUploadRequest = new FileUploadRequest(
                request.Request.FileStream,
                request.Request.FileName,
                request.Request.FileContentType ?? "application/octet-stream"); // default content type

            var fileUrl = await _fileService.UploadFileAsync(fileUploadRequest);

            var uploadedFile = new UploadedFile
            {
                FileName = request.Request.FileName,
                OriginalFileName = request.Request.FileName,
                Url = fileUrl,
                ContentType = request.Request.FileContentType!,
                Size = request.Request.FileStream.Length,
                DeckId = deckId,
                CreatedBy = _currentUserService.UserId
            };

            deck.Documents = new List<UploadedFile> { uploadedFile };
        }

        await _deckRepository.AddAsync(deck, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deck.Id;
    }
}
