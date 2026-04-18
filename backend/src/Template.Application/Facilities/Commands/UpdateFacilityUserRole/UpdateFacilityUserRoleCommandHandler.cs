using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Commands.UpdateFacilityUserRole;

public sealed class UpdateFacilityUserRoleCommandHandler(IAuthService authService) : IRequestHandler<UpdateFacilityUserRoleCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(UpdateFacilityUserRoleCommand request, CancellationToken ct)
    {
        var userResult = await _authService.GetUserByIdAsync(request.UserId, ct);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }

        if (userResult.Value.FacilityId != request.FacilityId)
        {
            return FacilityErrors.UserNotInFacility;
        }

        return await _authService.UpdateUserRoleAsync(request.UserId, request.NewRole, ct);
    }
}
