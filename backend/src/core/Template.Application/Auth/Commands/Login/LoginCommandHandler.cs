using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler(IAuthService authService, ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, ErrorOr<LoginResult>>
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<LoginCommandHandler> _logger = logger;

    public async ValueTask<ErrorOr<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Login attempt: {Email} / {Password}", request.Email, request.Password);
        return await _authService.LoginAsync(request.Email, request.Password, ct);
    }
}
