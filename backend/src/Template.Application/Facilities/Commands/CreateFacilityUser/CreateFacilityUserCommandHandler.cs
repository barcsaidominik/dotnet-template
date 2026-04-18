using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed class CreateFacilityUserCommandHandler(IAuthService authService) : IRequestHandler<CreateFacilityUserCommand, ErrorOr<CreateUserResult>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<CreateUserResult>> Handle(CreateFacilityUserCommand request, CancellationToken ct)
    {
        return await _authService.CreateFacilityUserAsync(request.Email, request.FacilityId, request.Role, ct);
    }
}
