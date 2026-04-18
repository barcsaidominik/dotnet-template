using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
        => _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => await _authService.RegisterAsync(request.Email, request.Password, cancellationToken);
}
