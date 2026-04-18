using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(IAuthService authService) : IRequestHandler<DeleteUserCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        return await _authService.DeleteUserAsync(request.UserId, ct);
    }
}
