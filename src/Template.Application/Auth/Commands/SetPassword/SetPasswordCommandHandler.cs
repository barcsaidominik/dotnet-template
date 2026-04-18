using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.SetPassword;

public sealed class SetPasswordCommandHandler : IRequestHandler<SetPasswordCommand, ErrorOr<Success>> {
    private readonly IAuthService _authService;

    public SetPasswordCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(SetPasswordCommand request, CancellationToken cancellationToken) {
        return await _authService.SetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
    }
}
