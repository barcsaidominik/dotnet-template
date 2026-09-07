using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Interfaces;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Commands.UpdateFacilityUserRole;

public sealed class UpdateFacilityUserRoleCommandHandler(
    IAuthService authService,
    ICurrentUserService currentUserService,
    IMemoryCache cache) : IRequestHandler<UpdateFacilityUserRoleCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<Success>> Handle(UpdateFacilityUserRoleCommand request, CancellationToken ct)
    {
        if (_currentUserService.Role != Roles.SYSTEM_ADMIN && _currentUserService.FacilityId != request.FacilityId)
        {
            return FacilityErrors.AccessDenied;
        }

        var userResult = await _authService.GetUserByIdAsync(request.UserId, ct);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }

        if (userResult.Value.FacilityId != request.FacilityId)
        {
            return FacilityErrors.UserNotInFacility;
        }

        var result = await _authService.UpdateUserRoleAsync(request.UserId, request.NewRole, ct);

        if (!result.IsError)
        {
            _cache.Remove(CacheKeys.ALL_USERS);
        }

        return result;
    }
}
