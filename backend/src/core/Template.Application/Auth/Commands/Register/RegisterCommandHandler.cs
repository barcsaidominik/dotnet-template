using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Interfaces;

namespace Template.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler(IAuthService authService, IMemoryCache cache) : IRequestHandler<RegisterCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<Success>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request.Email, request.Password, ct);

        if (!result.IsError)
        {
            _cache.Remove(CacheKeys.ALL_USERS);
        }

        return result;
    }
}
