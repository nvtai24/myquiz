using MediatR;

namespace MyQuizGenerator.Application.DeckInvitations.Commands.AcceptDeckInvitation;

public record AcceptDeckInvitationCommand(string Token) : IRequest<Guid>;
