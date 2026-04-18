using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, ErrorOr<Success>> {
    private readonly IAuthService _authService;

    public DeleteUserCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(DeleteUserCommand request, CancellationToken cancellationToken) {
        return await _authService.DeleteUserAsync(request.UserId, cancellationToken);
    }
}
