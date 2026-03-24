using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.DeckRatings.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.DeckRatings.Commands.CreateDeckRating;

public record CreateDeckRatingCommand(Guid DeckId, CreateDeckRatingRequest Request) : IRequest<Guid>;

public class CreateDeckRatingCommandHandler : IRequestHandler<CreateDeckRatingCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRatingRepository _deckRatingRepository;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDeckRatingCommandHandler(
        IUnitOfWork unitOfWork,
        IDeckRatingRepository deckRatingRepository,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _deckRatingRepository = deckRatingRepository;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateDeckRatingCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        // Validate rating range
        if (request.Request.Rating < 1 || request.Request.Rating > 5)
        {
            throw new BadRequestException("Rating must be between 1 and 5.");
        }

        // Check if deck exists
        var deck = await _deckRepository.GetByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException("Deck", request.DeckId);

        // Check if user already rated this deck
        var existingRating = await _deckRatingRepository.GetUserRatingForDeckAsync(request.DeckId, userId, cancellationToken);
        if (existingRating != null)
        {
            throw new BadRequestException("You have already rated this deck.");
        }

        // Create new rating
        var rating = new DeckRating
        {
            DeckId = request.DeckId,
            UserId = userId,
            Rating = request.Request.Rating,
            Comment = request.Request.Comment
        };

        await _deckRatingRepository.AddAsync(rating, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return rating.Id;
    }
}
