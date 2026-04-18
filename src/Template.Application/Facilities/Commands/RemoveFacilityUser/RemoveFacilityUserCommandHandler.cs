using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Commands.RemoveFacilityUser;

public sealed class RemoveFacilityUserCommandHandler : IRequestHandler<RemoveFacilityUserCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService;

    public RemoveFacilityUserCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(RemoveFacilityUserCommand request, CancellationToken cancellationToken)
    {
        var userResult = await _authService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (userResult.IsError)
            return userResult.Errors;

        if (userResult.Value.FacilityId != request.FacilityId)
            return FacilityErrors.UserNotInFacility;

        return await _authService.DeleteUserAsync(request.UserId, cancellationToken);
    }
}
