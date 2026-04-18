using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Facilities.Queries.GetFacilityUsers;

public sealed class GetFacilityUsersQueryHandler : IRequestHandler<GetFacilityUsersQuery, ErrorOr<IReadOnlyList<UserDto>>> {
    private readonly IAuthService _authService;

    public GetFacilityUsersQueryHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetFacilityUsersQuery request, CancellationToken cancellationToken) {
        return await _authService.GetFacilityUsersAsync(request.FacilityId, cancellationToken);
    }
}
