using MediatR;
using MyQuizGenerator.Application.Admin.DTOs;
using MyQuizGenerator.Application.Common.Interfaces;

namespace MyQuizGenerator.Application.Admin.Queries.GetUsers;

public record GetUsersQuery(int Page = 1, int PageSize = 10, string? Search = null)
    : IRequest<(List<AdminUserResponse> Users, int TotalCount)>;

public class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, (List<AdminUserResponse> Users, int TotalCount)>
{
    private readonly IAuthService _authService;

    public GetUsersQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<(List<AdminUserResponse> Users, int TotalCount)> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _authService.GetUsersAsync(request.Page, request.PageSize, request.Search);
    }
}
