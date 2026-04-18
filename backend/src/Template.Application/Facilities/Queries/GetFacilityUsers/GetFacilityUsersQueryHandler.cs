using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Facilities.Queries.GetFacilityUsers;

public sealed class GetFacilityUsersQueryHandler(IAuthService authService) : IRequestHandler<GetFacilityUsersQuery, ErrorOr<IReadOnlyList<UserDto>>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetFacilityUsersQuery request, CancellationToken ct)
    {
        return await _authService.GetFacilityUsersAsync(request.FacilityId, ct);
    }
}
