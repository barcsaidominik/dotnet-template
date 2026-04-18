using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler(IAuthService authService) : IRequestHandler<GetAllUsersQuery, ErrorOr<IReadOnlyList<UserDto>>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        return await _authService.GetAllUsersAsync(ct);
    }
}
