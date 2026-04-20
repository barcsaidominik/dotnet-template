using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Commands.ApproveUser;

public sealed class ApproveUserCommandHandler(IAuthService authService, IMemoryCache cache) : IRequestHandler<ApproveUserCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<Success>> Handle(ApproveUserCommand request, CancellationToken ct)
    {
        var result = await _authService.ApproveUserAsync(request.UserId, request.FacilityId, request.Role, ct);

        if (!result.IsError)
        {
            _cache.Remove(CacheKeys.ALL_USERS);
        }

        return result;
    }
}
