using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Commands.ApproveUser;

public sealed class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService;

    public ApproveUserCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(ApproveUserCommand request, CancellationToken cancellationToken)
        => await _authService.ApproveUserAsync(request.UserId, request.FacilityId, request.Role, cancellationToken);
}
