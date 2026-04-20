using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(IAuthService authService, ICurrentUserService currentUser, IMemoryCache cache) : IRequestHandler<DeleteUserCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<Success>> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        if (request.UserId == _currentUser.UserId)
        {
            return Error.Forbidden("Auth.SelfDelete", "You cannot delete your own account");
        }

        var result = await _authService.DeleteUserAsync(request.UserId, ct);

        if (!result.IsError)
        {
            _cache.Remove(CacheKeys.ALL_USERS);
        }

        return result;
    }
}
