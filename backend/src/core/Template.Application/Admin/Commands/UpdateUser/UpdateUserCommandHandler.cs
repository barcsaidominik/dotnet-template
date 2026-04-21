using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IAuthService authService, IMemoryCache cache) : IRequestHandler<UpdateUserCommand, ErrorOr<Updated>>
{
    private readonly IAuthService _authService = authService;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<Updated>> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var result = await _authService.UpdateUserAsync(request.UserId, request.Email, ct);

        if (!result.IsError)
        {
            _cache.Remove(CacheKeys.ALL_USERS);
        }

        return result;
    }
}
