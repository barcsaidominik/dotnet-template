using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed class CreateFacilityUserCommandHandler : IRequestHandler<CreateFacilityUserCommand, ErrorOr<CreateUserResult>>
{
    private readonly IAuthService _authService;

    public CreateFacilityUserCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<CreateUserResult>> Handle(CreateFacilityUserCommand request, CancellationToken cancellationToken)
        => await _authService.CreateFacilityUserAsync(request.Email, request.FacilityId, request.Role, cancellationToken);
}
