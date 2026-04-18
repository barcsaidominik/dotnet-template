using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, ErrorOr<IReadOnlyList<UserDto>>> {
    private readonly IAuthService _authService;

    public GetAllUsersQueryHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken) {
        return await _authService.GetAllUsersAsync(cancellationToken);
    }
}
