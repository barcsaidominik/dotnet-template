using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(IAuthService authService) : IRequestHandler<LoginCommand, ErrorOr<LoginResult>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        return await _authService.LoginAsync(request.Email, request.Password, ct);
    }
}
