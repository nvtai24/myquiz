using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Domain.Entities;
using MyQuizGenerator.Domain.Enums;

namespace MyQuizGenerator.Application.DeckInvitations.Commands.AcceptDeckInvitation;

public class AcceptDeckInvitationCommandHandler : IRequestHandler<AcceptDeckInvitationCommand, Guid>
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthService _authService;

    public AcceptDeckInvitationCommandHandler(
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IAuthService authService)
    {
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _authService = authService;
    }

    public async Task<Guid> Handle(AcceptDeckInvitationCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Token
        var invitation = await _deckRepository.GetInvitationByTokenAsync(request.Token, cancellationToken);
        if (invitation == null)
        {
            throw new NotFoundException(nameof(DeckInvitation), request.Token);
        }

        if (invitation.Status != DeckInvitationStatus.Pending)
        {
            throw new ValidationException(new List<string> { "Invitation has already been accepted or rejected." });
        }

        if (invitation.ExpiredAt < DateTime.UtcNow)
        {
            throw new ValidationException(new List<string> { "Invitation has expired." });
        }

        // 2. Get Current User
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            // If user is not logged in, we might want to return some specific error or handle it.
            // But usually for acceptance, they should be logged in or we register them.
            // Assuming they are logged in for now as per requirement context "Accept Invitation API".
            throw new UnauthorizedAccessException();
        }


        if (!string.Equals(_currentUserService.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("This invitation was sent to another email address.");
        }

        // 3. Create DeckMember
        var deckMember = new DeckMember
        {
            DeckId = invitation.DeckId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };

        // We need a repository for DeckMember? 
        // Or we can add it via Deck relationship if we fetch deck. But simpler to add to context via generic repo or deck repo.
        // Let's assume we can add it via DbContext directly or we add a method to DeckRepository.
        // Checking IDeckRepository... it's specific to Deck.
        // Let's add AddMemberAsync to IDeckRepository or use IUnitOfWork if it exposes generic context.
        // For now, I'll add `AddMemberAsync` to IDeckRepository separately or usage `_context` in repository.
        // Wait, I can't modify repository here.
        // I should have added AddDeckMember to repository.
        // Let me assume I can add it to the Deck's collection if I fetch the deck? 
        // Or better, let's look at how IDeckRepository is implemented.
        // It has AddInvitationAsync. I should probably add AddDeckMemberAsync.

        // Let's add the method to repository first in next step if it's missing.
        // But for now, I will write this handler assuming I can save it.
        // Actually, I can use the UnitOfWork if I had a generic repository, but IDeckRepository is specific.

        // Use generic approach for now if possible? No, sticking to repository pattern.
        // I will add `AddDeckMemberAsync` to `IDeckRepository` in a parallel step or update implementation.
        // But wait, `DeckMember` is a tracked entity.

        // Re-reading `IDeckRepository`.
        // I will use `_unitOfWork` if possible, but Clean Architecture usually goes through Repositories.
        // I'll add `Task AddDeckMemberAsync(DeckMember member, ...)` into `IDeckRepository`.

        // For this file content, I'll assume method `AddDeckMemberAsync` exists.

        await _deckRepository.AddDeckMemberAsync(deckMember, cancellationToken);

        // 4. Update Invitation Status
        invitation.Status = DeckInvitationStatus.Accepted;
        // Entity is tracked, so just SaveChanges.

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deckMember.Id;
    }
}
