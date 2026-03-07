using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Application.Files.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Commands.UpdateDeck;

public record UpdateDeckCommand(Guid Id, UpdateDeckRequest Request) : IRequest;

public class UpdateDeckCommandHandler : IRequestHandler<UpdateDeckCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileService _fileService;

    public UpdateDeckCommandHandler(
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

    public async Task Handle(UpdateDeckCommand request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetByIdAsync(request.Id, cancellationToken);

        if (deck == null)
        {
            throw new NotFoundException(nameof(Deck), request.Id);
        }

        if (deck.OwnerId != _currentUserService.UserId)
        {
            throw new ForbiddenException("You do not have permission to update this deck.");
        }

        deck.Name = request.Request.Name;
        deck.Description = request.Request.Description;
        deck.Visibility = request.Request.Visibility;
        deck.Tags = request.Request.Tags;
        deck.UpdatedAt = DateTime.UtcNow;

        // Upload thumbnail file and set URL if provided
        if (request.Request.ThumbnailStream != null && !string.IsNullOrEmpty(request.Request.ThumbnailFileName))
        {
            var thumbnailUploadRequest = new FileUploadRequest(
                request.Request.ThumbnailStream,
                request.Request.ThumbnailFileName,
                request.Request.ThumbnailContentType ?? "image/png");

            var thumbnailUrl = await _fileService.UploadFileAsync(thumbnailUploadRequest);
            deck.ThumbnailUrl = thumbnailUrl;
        }

        _deckRepository.Update(deck);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
