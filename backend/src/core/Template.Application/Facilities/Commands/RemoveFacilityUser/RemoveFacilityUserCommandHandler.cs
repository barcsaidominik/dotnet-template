using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Commands.RemoveFacilityUser;

public sealed class RemoveFacilityUserCommandHandler(
    IAuthService authService,
    ICurrentUserService currentUserService) : IRequestHandler<RemoveFacilityUserCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<Success>> Handle(RemoveFacilityUserCommand request, CancellationToken ct)
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

        return await _authService.DeleteUserAsync(request.UserId, ct);
    }
}
