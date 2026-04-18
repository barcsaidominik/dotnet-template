using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler(IAuthService authService) : IRequestHandler<RefreshCommand, ErrorOr<LoginResult>>
{
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<LoginResult>> Handle(RefreshCommand request, CancellationToken ct)
    {
        return await _authService.RefreshAsync(request.RefreshToken, ct);
    }
}
