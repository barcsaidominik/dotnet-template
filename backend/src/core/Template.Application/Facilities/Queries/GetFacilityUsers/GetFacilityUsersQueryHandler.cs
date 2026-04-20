using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Queries.GetFacilityUsers;

public sealed class GetFacilityUsersQueryHandler(
    IAuthService authService,
    ICurrentUserService currentUserService) : IRequestHandler<GetFacilityUsersQuery, ErrorOr<IReadOnlyList<UserDto>>>
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetFacilityUsersQuery request, CancellationToken ct)
    {
        if (_currentUserService.Role != Roles.SYSTEM_ADMIN && _currentUserService.FacilityId != request.FacilityId)
        {
            return FacilityErrors.AccessDenied;
        }

        return await _authService.GetFacilityUsersAsync(request.FacilityId, ct);
    }
}
