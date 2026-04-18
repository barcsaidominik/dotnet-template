using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.SetPassword;

public sealed class SetPasswordCommandHandler(IAuthService authService) : IRequestHandler<SetPasswordCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(SetPasswordCommand request, CancellationToken ct)
    {
        return await _authService.SetPasswordAsync(request.Email, request.Token, request.NewPassword, ct);
    }
}
