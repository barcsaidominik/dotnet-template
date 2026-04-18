using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, ErrorOr<LoginResult>>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        => await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
}
