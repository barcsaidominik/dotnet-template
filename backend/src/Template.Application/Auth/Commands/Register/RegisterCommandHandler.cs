using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(IAuthService authService) : IRequestHandler<RegisterCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(RegisterCommand request, CancellationToken ct)
    {
        return await _authService.RegisterAsync(request.Email, request.Password, ct);
    }
}
