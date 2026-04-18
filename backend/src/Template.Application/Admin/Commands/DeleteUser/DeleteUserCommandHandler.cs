using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(IAuthService authService, ICurrentUserService currentUser) : IRequestHandler<DeleteUserCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async ValueTask<ErrorOr<Success>> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        if (request.UserId == _currentUser.UserId)
        {
            return Error.Forbidden("Auth.SelfDelete", "You cannot delete your own account");
        }

        return await _authService.DeleteUserAsync(request.UserId, ct);
    }
}
