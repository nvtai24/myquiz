using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;
using System.Text.RegularExpressions;

namespace MyQuizGenerator.Application.DeckInvitations.Commands.CreateDeckInvitation;

public record CreateDeckInvitationCommand(Guid DeckId, string Email) : IRequest<Guid>;

public class CreateDeckInvitationCommandHandler : IRequestHandler<CreateDeckInvitationCommand, Guid>
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public CreateDeckInvitationCommandHandler(
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService,
        IAuthService authService,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
        _authService = authService;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<Guid> Handle(CreateDeckInvitationCommand request, CancellationToken cancellationToken)
    {

        // 1. Get current user
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new UnauthorizedAccessException();
        }

        // 2. Get deck and validate ownership
        var deck = await _deckRepository.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck == null)
        {
            throw new NotFoundException(nameof(Deck), request.DeckId);
        }

        if (deck.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You are not the owner of this deck.");
        }

        // Optional: Check if email is self (needs extra call, skipping for performance or adding later if needed)
        // var currentUser = await _authService.GetUserByIdAsync(currentUserId);
        // if (currentUser?.Email == request.Email) throw new ValidationException...

        // 3. Check if invitation already exists (by Email)
        var exists = await _deckRepository.HasInvitationAsync(request.DeckId, request.Email, cancellationToken);
        if (exists)
        {
            throw new ValidationException(new List<string> { "User is already invited to this deck." });
        }

        // 4. Create invitation with Token
        var token = Guid.NewGuid().ToString("N");
        var invitation = new DeckInvitation
        {
            DeckId = request.DeckId,
            Email = request.Email,
            Token = token,
            SharedAt = DateTime.UtcNow,
            // ExpiredAt default is handled in entity or we set it here explicity
            ExpiredAt = DateTime.UtcNow.AddDays(7),
            Status = DeckInvitationStatus.Pending
        };

        // 5. Save
        await _deckRepository.AddInvitationAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Send Email
        await _emailService.SendDeckInvitationEmailAsync(request.Email, deck.Name, token, cancellationToken);

        return invitation.Id;
    }
}
