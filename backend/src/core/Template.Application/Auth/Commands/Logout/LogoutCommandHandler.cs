using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(IAuthService authService) : IRequestHandler<LogoutCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<Success>> Handle(LogoutCommand request, CancellationToken ct)
    {
        return await _authService.LogoutAsync(request.RefreshToken, ct);
    }
}
