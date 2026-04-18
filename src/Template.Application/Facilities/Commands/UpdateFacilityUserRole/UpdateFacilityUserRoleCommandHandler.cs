using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Commands.UpdateFacilityUserRole;

public sealed class UpdateFacilityUserRoleCommandHandler : IRequestHandler<UpdateFacilityUserRoleCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService;

    public UpdateFacilityUserRoleCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(UpdateFacilityUserRoleCommand request, CancellationToken cancellationToken)
    {
        var userResult = await _authService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (userResult.IsError)
            return userResult.Errors;

        if (userResult.Value.FacilityId != request.FacilityId)
            return FacilityErrors.UserNotInFacility;

        return await _authService.UpdateUserRoleAsync(request.UserId, request.NewRole, cancellationToken);
    }
}
