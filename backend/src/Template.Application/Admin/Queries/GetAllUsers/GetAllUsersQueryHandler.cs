using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler(IAuthService authService, IMemoryCache cache) : IRequestHandler<GetAllUsersQuery, ErrorOr<IReadOnlyList<UserDto>>>
{
    private readonly IAuthService _authService = authService;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKeys.ALL_USERS, out IReadOnlyList<UserDto>? cached))
        {
            return cached!.ToList();
        }

        var result = await _authService.GetAllUsersAsync(ct);

        if (!result.IsError)
        {
            _cache.Set(CacheKeys.ALL_USERS, result.Value, TimeSpan.FromMinutes(5));
        }

        return result;
    }
}
