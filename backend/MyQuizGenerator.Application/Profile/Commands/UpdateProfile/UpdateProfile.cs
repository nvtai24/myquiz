using MediatR;
using MyQuizGenerator.Application.Auth.DTOs;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Profile.DTOs;

namespace MyQuizGenerator.Application.Profile.Commands.UpdateProfile;

public record UpdateProfileCommand(string UserId, UpdateProfileRequest Request, string? AvatarUrl) : IRequest<UserResponse>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserResponse>
{
    private readonly IAuthService _authService;

    public UpdateProfileCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<UserResponse> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var updatedUser = await _authService.UpdateProfileAsync(
            command.UserId,
            command.Request.FirstName,
            command.Request.LastName,
            command.AvatarUrl);

        if (updatedUser == null)
        {
            throw new NotFoundException("User", command.UserId);
        }

        return updatedUser;
    }
}
